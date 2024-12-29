using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Discord.Audio;
using Microsoft.Extensions.Caching.Memory;

namespace DiscordBot;

public class PlaylistController
{
    private readonly Bot _bot;
    private static IMemoryCache _memoryCache = null!;
    private static int? _songIndex = null;

    private static CancellationTokenSource _cancellationTokenSource = new();

    public static string? SongName { get; private set; } = null;
    public static bool IsSongPlaying { get; private set; }

    public PlaylistController(Bot bot, IMemoryCache memoryCache)
    {
        _bot = bot;
        _memoryCache = memoryCache;

        _bot.PlaybackChanged += BotOnPlaybackChanged;
    }

    private async Task BotOnPlaybackChanged(PlaybackChangeType change, int? songIndex, bool repeat)
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
                
                var file = GetFileList()[(int)songIndex];
                _songIndex = songIndex;
                SongName = file;
                
                try
                {
                    var wasRepeat = _bot.Repeat;
                    do
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        IsSongPlaying = true;
                        
                        await PlayAudioAsync(Path.GetFullPath($"{_soundDirectory}/{file}"), _cancellationTokenSource.Token);
                        Thread.Sleep(1000);
                        // await _bot.StopPlayback();
                    } while (_bot.Repeat);

                    if (!wasRepeat)
                    {
                        if (_bot.LastTextChannel is not null)
                            await _bot.LastTextChannel.SendMessageAsync($"Играю {GetNextSongName()} (автоматический next)");
                        await _bot.NextPlayback();
                    }
                }
                catch (Exception e)
                {
                    if (_bot.LastTextChannel is not null)
                        await _bot.LastTextChannel.SendMessageAsync($"Произошла ошибка во время произведения {file}: {e.Message}");
                    return;
                }
                break;
            case PlaybackChangeType.Stop:
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource = new CancellationTokenSource();
                
                IsSongPlaying = false;
                
                break;
            case PlaybackChangeType.Leave:
                await _bot.StopPlayback();
                
                _songIndex = null;
                SongName = null;
                await _bot.ResetAudioClient();
                _bot.LastTextChannel = null;
                IsSongPlaying = false;

                break;
            case PlaybackChangeType.Next:
                if (_songIndex is null) return;
                var nextIndex = GetNextSongIndex();
                
                await _bot.PlayNewSong(nextIndex);
                
                break;
            case PlaybackChangeType.Previous:
                if (_songIndex is null) return;
                var prevIndex = GetPreviousSongIndex();
                
                await _bot.PlayNewSong(prevIndex);
                
                break;
            
            case PlaybackChangeType.SetRepeat:
                _bot.Repeat = repeat;
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(change), change, null);
        }
    }
    
        
    private async Task PlayAudioAsync(string path, CancellationToken token)
    {
        if (_bot.AudioClient is null)
            throw new Exception("Не подключен к какому-либо каналу");
        
        await SendAsync(_bot.AudioClient, path, token);
    }
    
    private async Task SendAsync(IAudioClient client, string path, CancellationToken token)
    {
        // Create FFmpeg using the previous example
        using var ffmpeg = CreateStream(path);
        await using var output = ffmpeg.StandardOutput.BaseStream;
        await using var discord = client.CreatePCMStream(AudioApplication.Mixed);
        
        try { await output.CopyToAsync(discord, token); }
        finally { await discord.FlushAsync(CancellationToken.None); }
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
            .FindIndex(x => x.Split(".", 2)[1].Contains(query, StringComparison.CurrentCultureIgnoreCase));

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
    
    public static List<string> GetFileList()
    {
        _memoryCache.TryGetValue("fileList", out List<string>? fileList);
        if (fileList is null)
        {
            MemoryCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
            };

            fileList = Directory.GetFiles(_soundDirectory)
                .Select(Path.GetFileName)
                .Order()
                .ToList()!;

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