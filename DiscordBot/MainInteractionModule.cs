using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.Interactions;

namespace DiscordBot;

public class MainInteractionModule(Bot bot) : InteractionModuleBase
{
    public static ulong? TestChannelId { get; private set; } = null;
    public static IAudioClient? AudioClient { get; private set; } = null;
    private static AudioOutStream? _audioOutStream = null;

    private static int? _lastPlaylistIndex = null;

    public static IMessageChannel? LastTextChannel { get; private set; } = null;
    
    [SlashCommand("echo", "Тестовая команда для проверки бота")]
    public async Task Echo(string input)
    {
        await RespondAsync(input);
    }

    [SlashCommand("test-remember-channel", "[Админ] Данная команда нужна для проверки связки между Overlord и DiscordBot")]
    public async Task TestRememberChannel(ITextChannel channel)
    {
        TestChannelId = channel.Id;
        await RespondAsync(text: $"Успешно запомнил канал '{TestChannelId}'", ephemeral: true);
    }
    
    [SlashCommand("join", "Да начнётся треш в голосовом канале!", runMode: RunMode.Async)]
    public async Task Join()
    {
        var user = Context.User as IGuildUser;
        if (user!.VoiceChannel == null)
        {
            await RespondAsync("Дядя, в канал-то зайди голосовой ежжи", ephemeral: true);
            return;
        }

        LastTextChannel = Context.Channel;

        try
        {
            await RespondAsync("Стартуем!");
            AudioClient = await user.VoiceChannel.ConnectAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error while connecting to a VC: " + e.Message);
        }
    }

    private const string SoundDirectory = "../FileWebServer/uploads";

    // [SlashCommand("play-sound", "Играет звук из папки со звуками бота", runMode: RunMode.Async)]
    // public async Task PlaySound([Summary(name: "file-name", description: "Имя файла")] string fileName)
    // {
    //     await DeferAsync();
    //     
    //     try
    //     {
    //         await PlayAudioAsync(Path.GetFullPath($"{SoundDirectory}/{fileName}"));
    //     }
    //     catch (Exception e)
    //     {
    //         await ModifyOriginalResponseAsync(x => x.Content = $"Произошла ошибка: {e.Message}");
    //         return;
    //     }
    //
    //     await ModifyOriginalResponseAsync(x => x.Content= $"Звук '{fileName}' проигран");
    // }

    [SlashCommand("vlad", "Играет из великого плейлиста 'Бамбузлинг в машину'", runMode: RunMode.Async)]
    public async Task PlayFromPlaylist([Summary(name: "name", description: "Поиск этого имени по плейлисту")] string name)
    {
        await DeferAsync();

        var file = Directory.GetFiles(SoundDirectory)
            .ToList()
            .ConvertAll(Path.GetFileName)
            .FirstOrDefault(x => x?.Contains(name, StringComparison.CurrentCultureIgnoreCase) == true);

        if (file is null)
        {
            await ModifyOriginalResponseAsync(x => x.Content = $"Не удалось найти трек с таким названием :(");
            return;
        }

        if (!int.TryParse(file.Split('.')[0], out var parsedIndex))
        {
            _lastPlaylistIndex = 1;
            await ReplyAsync($"Не смог пропарсить индекс в плейлисте ({file.Split(".")[0]})");
        }

        _lastPlaylistIndex = parsedIndex;
        
        await ModifyOriginalResponseAsync(x => x.Content= $"Играю: {file}");
        bot.PlayNewSong((int)_lastPlaylistIndex);
        
        try
        {
            await PlayAudioAsync(Path.GetFullPath($"{SoundDirectory}/{file}"));
        }
        catch (Exception e)
        {
            await ModifyOriginalResponseAsync(x => x.Content = $"Произошла ошибка: {e.Message}");
            return;
        }
    }
    
    [SlashCommand("stop", "Останавливает текущую песню", runMode: RunMode.Async)]
    public async Task Stop()
    {
        if (AudioClient != null)
        {
            if (_audioOutStream is not null)
            {
                await _audioOutStream.DisposeAsync();
                _audioOutStream = null;
            }

            // Disconnect the audio client
            await RespondAsync("Остановил песню");
            bot.StopPlayback();
        }
        else
        {
            await RespondAsync("Дядя, у тебя шиза, я даже к каналу не подключен никакому", ephemeral: true);
        }
    }

    [SlashCommand("next", "Переключает на следующую песню", runMode: RunMode.Async)]
    public async Task Next()
    {
        await DeferAsync();
        
        if (_lastPlaylistIndex is null)
        {
            await RespondAsync("Шиз, я не играю сейчас ничего");
            return;
        }
        
        var files = Directory.GetFiles(SoundDirectory)
            .ToList()
            .ConvertAll(Path.GetFileName);
        
        var currentIndex = files
            .FindIndex(x => int.Parse(x.Split('.')[0]) == _lastPlaylistIndex);

        if (currentIndex > files.Count)
        {
            await ModifyOriginalResponseAsync(x => x.Content = $"Не удалось переключиться вперед :(");
            return;
        }

        var file = files[currentIndex + 1];
        _lastPlaylistIndex = currentIndex + 1;
        
        await ModifyOriginalResponseAsync(x => x.Content= $"Играю: {file}");
        bot.NextPlayback();
        
        try
        {
            await PlayAudioAsync(Path.GetFullPath($"{SoundDirectory}/{file}"));
        }
        catch (Exception e)
        {
            await ModifyOriginalResponseAsync(x => x.Content = $"Произошла ошибка: {e.Message}");
            return;
        }
    }
    
    [SlashCommand("prev", "Переключает на предыдущую песню", runMode: RunMode.Async)]
    public async Task Previous()
    {
        await DeferAsync();
        
        if (_lastPlaylistIndex is null)
        {
            await RespondAsync("Шиз, я не играю сейчас ничего");
            return;
        }

        var files = Directory.GetFiles(SoundDirectory);
        _lastPlaylistIndex--;
        // TODO: Min Index
        
        var file = files
            .ToList()
            .ConvertAll(Path.GetFileName)
            .FirstOrDefault(x => int.Parse(x.Split('.')[0]) == _lastPlaylistIndex);

        if (file is null)
        {
            await ModifyOriginalResponseAsync(x => x.Content = $"Не удалось переключиться назад :(");
            return;
        }
        
        await ModifyOriginalResponseAsync(x => x.Content= $"Играю: {file}");
        bot.PreviousPlayback();
        
        try
        {
            await PlayAudioAsync(Path.GetFullPath($"{SoundDirectory}/{file}"));
        }
        catch (Exception e)
        {
            await ModifyOriginalResponseAsync(x => x.Content = $"Произошла ошибка: {e.Message}");
            return;
        }
    }
    
    public async Task PlayAudioAsync(string path)
    {
        if (AudioClient is null)
            throw new Exception("Не подключен к какому-либо каналу");

        if (_audioOutStream is not null)
        {
            await _audioOutStream.DisposeAsync();
            _audioOutStream = null;
        }
        
        await SendAsync(AudioClient, path);
    }
    
    private async Task SendAsync(IAudioClient client, string path)
    {
        // Create FFmpeg using the previous example
        using (var ffmpeg = CreateStream(path))
        using (var output = ffmpeg.StandardOutput.BaseStream)
        {
            _audioOutStream ??= client.CreatePCMStream(AudioApplication.Mixed);
            try
            {
                await output.CopyToAsync(_audioOutStream);

                Thread.Sleep(2000);
                
                #region Next

                var files = Directory.GetFiles(SoundDirectory)
                    .ToList()
                    .ConvertAll(Path.GetFileName);
        
                var currentIndex = files
                    .FindIndex(x => int.Parse(x.Split('.')[0]) == _lastPlaylistIndex);

                if (currentIndex > files.Count)
                    return;

                var file = files[currentIndex + 1];
                _lastPlaylistIndex = currentIndex + 1;

                if (LastTextChannel is not null)
                    await LastTextChannel.SendMessageAsync($"Играю {file}");
        
                try
                {
                    await PlayAudioAsync(Path.GetFullPath($"{SoundDirectory}/{file}"));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Произошла ошибка: {e.Message}");
                    return;
                }

                #endregion
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while sending audio: {e.Message}");
                throw;
            }
            finally
            {
                await _audioOutStream.FlushAsync();
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
    
    [SlashCommand("leave", "Хватит на сегодня интернета")]
    public async Task Leave()
    {
        // var audioClient = (Context.Channel as IGuildChannel)!.Guild.AudioClient;

        if (AudioClient != null)
        {
            // Stop the audio client if needed (e.g., if it's playing or streaming something)
            // ...

            if (_audioOutStream is not null)
            {
                await _audioOutStream.DisposeAsync();
                _audioOutStream = null;
            }

            // Disconnect the audio client
            await AudioClient.StopAsync();
            await RespondAsync("А на сегодня всё...");

            _lastPlaylistIndex = null;
            AudioClient = null;
            LastTextChannel = null;
        }
        else
        {
            await RespondAsync("Дядя, у тебя шиза, я даже к каналу не подключен никакому", ephemeral: true);
        }
    }
}