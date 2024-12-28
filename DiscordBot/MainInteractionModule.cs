using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.Interactions;

namespace DiscordBot;

public class MainInteractionModule(Bot bot) : InteractionModuleBase
{
    [SlashCommand("join", "Да начнётся треш в голосовом канале!", runMode: RunMode.Async)]
    public async Task Join()
    {
        await ConnectToVoice();
        await RespondAsync("Успешно");
    }

    private async Task ConnectToVoice()
    {
        bot.LastTextChannel = Context.Channel;
        
        var user = Context.User as IGuildUser;
        if (user!.VoiceChannel == null)
        {
            await bot.LastTextChannel.SendMessageAsync("Дядя, в канал-то зайди голосовой ежжи");
            return;
        }

        try
        {
            await bot.LastTextChannel.SendMessageAsync("Стартуем!");
            bot.AudioClient = await user.VoiceChannel.ConnectAsync();
        }
        catch (Exception e)
        {
           await bot.LastTextChannel.SendMessageAsync("Error while connecting to a VC: " + e.Message);
        }
    }

    [SlashCommand("vlad", "Играет из великого плейлиста 'Бамбузлинг в машину'", runMode: RunMode.Async)]
    public async Task PlayFromPlaylist([Summary(name: "name", description: "Поиск этого имени по плейлисту")] string name)
    {
        var index = PlaylistController.FindSongIndex(name, out var file);

        if (file is null)
        {
            await RespondAsync("Не удалось найти трек с таким названием :(");
            return;
        }

        if (bot.AudioClient is null)
            await ConnectToVoice();
        
        await RespondAsync($"Играю: {file}");
        await bot.PlayNewSong(index);
    }

    [SlashCommand("vlad-prefix", "Играет из великого плейлиста 'Бамбузлинг в машину' по префиксу в плейлисте", runMode: RunMode.Async)]
    public async Task PlayByPrefix([Summary(name: "prefix", description: "Префикс песни (с 1)")] int prefix)
    {
        var index = PlaylistController.FindSongIndex(prefix, out var file);

        if (file is null)
        {
            await RespondAsync("Не удалось найти трек с таким названием :(");
            return;
        }
        
        if (bot.AudioClient is null)
            await ConnectToVoice();
        
        await RespondAsync($"Играю: {file}");
        await bot.PlayNewSong(index);
    }

    [SlashCommand("random", "Играет случайную песню из 'Бамбузлинг в машину'", runMode: RunMode.Async)]
    public async Task PlayRandomSong()
    {
        await RespondAsync("Пока не работает ( •̀ ω •́ )y");
    }
    
    [SlashCommand("song-list", "Показывает список доступных песен в плейлисте 'Бамбузлинг в машину'", runMode: RunMode.Async)]
    public async Task SongList()
    {
        await RespondAsync("Пока не работает ( •̀ ω •́ )y");
    }
    
    [SlashCommand("stop", "Останавливает текущую песню", runMode: RunMode.Async)]
    public async Task Stop()
    {
        if (bot.AudioClient != null)
        {
            await RespondAsync("Остановил песню");
            await bot.StopPlayback();
        }
        else
        {
            await RespondAsync("Дядя, у тебя шиза, я даже к каналу не подключен никакому", ephemeral: true);
        }
    }

    [SlashCommand("next", "Переключает на следующую песню", runMode: RunMode.Async)]
    public async Task Next()
    {
        if (bot.AudioOutStream is null)
        {
            await RespondAsync("Шиз, я не играю сейчас ничего");
            return;
        }

        var file = PlaylistController.GetNextSongName();
        if (file is null)
        {
            await RespondAsync("Не смог включить следующую песню :(");
            return;
        }
        
        if (!bot.Repeat)
            await RespondAsync($"Играю: {file}");
        else
        {
            bot.Repeat = false;
            await RespondAsync($"Играю: {file} (Сбросил флаг повтора песен)");
        }
        await bot.NextPlayback();
    }
    
    [SlashCommand("prev", "Переключает на предыдущую песню", runMode: RunMode.Async)]
    public async Task Previous()
    {
        if (bot.AudioOutStream is null)
        {
            await RespondAsync("Шиз, я не играю сейчас ничего");
            return;
        }

        var file = PlaylistController.GetPreviousSongName();
        if (file is null)
        {
            await RespondAsync("Не смог включить предыдущую песню :(");
            return;
        }
        
        if (!bot.Repeat)
            await RespondAsync($"Играю: {file}");
        else
        {
            bot.Repeat = false;
            await RespondAsync($"Играю: {file} (Сбросил флаг повтора песен)");
        }
        await bot.PreviousPlayback();
    }

    [SlashCommand("repeat", "Повторять текущий трек без конца (сбрасывается, если поменять трек)")]
    public async Task Repeat()
    {
        if (bot.Repeat)
        {
            await RespondAsync("Повтор трека выключен");
            bot.Repeat = false;
        }
        else
        {
            await RespondAsync("Повтор трека включен");
            bot.Repeat = true;
        }
    }
    
    [SlashCommand("leave", "Хватит на сегодня интернета")]
    public async Task Leave()
    {
        if (bot.AudioClient != null)
        {
            await bot.LeaveChannel();
            await RespondAsync("А на сегодня всё...");
        }
        else
        {
            await RespondAsync("Дядя, у тебя шиза, я даже к каналу не подключен никакому", ephemeral: true);
        }
    }
}