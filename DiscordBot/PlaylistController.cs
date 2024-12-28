﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Discord.Audio;
using Microsoft.Extensions.Caching.Memory;

namespace DiscordBot;

public class PlaylistController
{
    private readonly Bot _bot;
    private static IMemoryCache _memoryCache = null!;
    private static int? _songIndex = null;

    public PlaylistController(Bot bot, IMemoryCache memoryCache)
    {
        _bot = bot;
        _memoryCache = memoryCache;

        _bot.PlaybackChanged += BotOnPlaybackChanged;
    }

    private async Task BotOnPlaybackChanged(PlaybackChangeType change, int? songIndex)
    {
        Console.WriteLine($"!{change.ToString()}: {songIndex}");
        switch (change)
        {
            case PlaybackChangeType.NewSong:
                if (songIndex is null)
                {
                    if (_bot.LastTextChannel is not null)
                        await _bot.LastTextChannel.SendMessageAsync("Не был указан индекс при PlaybackChangeType.NewSong");
                    return;
                }

                if (_bot.AudioOutStream is not null)
                    await _bot.StopPlayback();
                
                var file = GetFileList()[(int)songIndex];
                _songIndex = songIndex;
                
                try
                {
                    var wasRepeat = _bot.Repeat;
                    do
                    {
                        await PlayAudioAsync(Path.GetFullPath($"{_soundDirectory}/{file}"));
                        Thread.Sleep(1000);
                        await _bot.StopPlayback();
                    } while (_bot.Repeat);

                    if (!wasRepeat)
                        await _bot.NextPlayback();
                }
                catch (Exception e)
                {
                    if (_bot.LastTextChannel is not null)
                        await _bot.LastTextChannel.SendMessageAsync($"Произошла ошибка во время произведения {file}: {e.Message}");
                    return;
                }
                break;
            case PlaybackChangeType.Stop:
                if (_bot.AudioOutStream is not null)
                {
                    await _bot.AudioOutStream.DisposeAsync();
                    _bot.AudioOutStream = null;
                }
                
                break;
            case PlaybackChangeType.Leave:
                if (_bot.AudioOutStream is not null) 
                    await _bot.AudioOutStream.DisposeAsync();
                
                if (_bot.AudioClient != null)
                    await _bot.AudioClient.StopAsync();
                
                _songIndex = null;
                _bot.AudioClient = null;
                _bot.LastTextChannel = null;

                break;
            case PlaybackChangeType.Next:
                if (_songIndex is null) return;
                var nextIndex = GetNextSongIndex();

                if (_bot.AudioOutStream is not null)
                    await _bot.StopPlayback();
                
                await _bot.PlayNewSong(nextIndex);
                
                break;
            case PlaybackChangeType.Previous:
                if (_songIndex is null) return;
                var prevIndex = GetPreviousSongIndex();

                if (_bot.AudioOutStream is not null)
                    await _bot.StopPlayback();
                
                await _bot.PlayNewSong(prevIndex);
                
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(change), change, null);
        }
    }
    
        
    private async Task PlayAudioAsync(string path)
    {
        if (_bot.AudioClient is null)
            throw new Exception("Не подключен к какому-либо каналу");

        if (_bot.AudioOutStream is not null)
        {
            await _bot.AudioOutStream.DisposeAsync();
            _bot.AudioOutStream = null;
        }
        
        await SendAsync(_bot.AudioClient, path);
    }
    
    private async Task SendAsync(IAudioClient client, string path)
    {
        // Create FFmpeg using the previous example
        using (var ffmpeg = CreateStream(path))
        using (var output = ffmpeg.StandardOutput.BaseStream)
        {
            _bot.AudioOutStream ??= client.CreatePCMStream(AudioApplication.Mixed);
            try
            {
                await output.CopyToAsync(_bot.AudioOutStream);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while sending audio: {e.Message}");
                throw;
            }
            finally
            {
                await _bot.AudioOutStream.FlushAsync();
            }
        }
    }
    
    private static Process CreateStream(string path)
    {
        return Process.Start(new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
        }) ?? throw new Exception("Не смог создать Process (Process.Start вернул null)");
    }

    
    private static string _soundDirectory = "../FileWebServer/uploads";
    
    public static int FindSongIndex(string query, out string? fileName)
    {
        var fileList = GetFileList();
        var index = fileList
            .FindIndex(x => x.Split(".")[1].Contains(query, StringComparison.CurrentCultureIgnoreCase));

        if (index == -1)
        {
            fileName = null;
            return -1;
        }
        
        fileName = fileList[index];
        
        return index;
    }

    public static int FindSongIndex(int prefix, out string? fileName)
    {
        var fileList = GetFileList();
        var index = fileList
            .FindIndex(x => x.Split(".")[0].Contains(prefix.ToString()));

        if (index == -1)
        {
            fileName = null;
            return -1;
        }
        
        fileName = fileList[index];
        
        return index;
    }
    
    private static List<string> GetFileList()
    {
        _memoryCache.TryGetValue("fileList", out List<string>? fileList);
        if (fileList is null)
        {
            MemoryCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };

            fileList = Directory.GetFiles(_soundDirectory)
                .ToList()
                .ConvertAll(Path.GetFileName)!;

            _memoryCache.Set("fileList", fileList, options);
        }

        return fileList;
    }

    public static string? GetNextSongName()
    {
        if (_songIndex is null) return null;
        var nextIndex = GetNextSongIndex();

        return GetFileList()[nextIndex];
    }

    private static int GetNextSongIndex()
    {
        var nextIndex = _songIndex + 1;
        if (nextIndex >= GetFileList().Count) nextIndex = 0;
        return (int)nextIndex!;
    }

    public static string? GetPreviousSongName()
    {
        if (_songIndex is null) return null;
        var nextIndex = GetPreviousSongIndex();

        return GetFileList()[nextIndex];
    }

    private static int GetPreviousSongIndex()
    {
        var nextIndex = _songIndex - 1;
        if (nextIndex < 0) nextIndex = GetFileList().Count - 1;
        return (int)nextIndex!;
    }
}