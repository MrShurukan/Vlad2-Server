using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot;

public class Bot(IServiceProvider serviceProvider)
{
    public IAudioClient? AudioClient { get; set; } = null;
    public AudioOutStream? AudioOutStream = null;
    public bool Repeat = false;
    public string TrackName { get; private set; }
    
    public IMessageChannel? LastTextChannel { get; set; } = null;

    public DiscordSocketClient Client { get; private set; } = new();
    private InteractionService _interactionService = default!;
    public Settings Settings { get; private set; }
    
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    public TrackStatus GetTrackStatus()
    {
        return new TrackStatus()
        {
            Name = PlaylistController.SongName,
            IsRepeat = Repeat,
            IsSongPlaying = PlaylistController.SongName is not null
        };
    }
    
    public void UpdateSettings(Settings discordSettings)
    {
        Settings = discordSettings;
    }

    public async Task StartAsync()
    {
        Client.Log += Log;
        // _client.Ready += ClientReady;
        // _client.SlashCommandExecuted += SlashCommandHandler;

        _interactionService = new InteractionService(Client.Rest);
        try
        {
            await _interactionService.AddModulesAsync(typeof(MainInteractionModule).Assembly, serviceProvider);
        }
        catch (Exception e)
        {
            Console.WriteLine("\n\nERROR WHILE ADDING MODULES: " + e.Message);
            throw;
        }

        Client.InteractionCreated += async (x) =>
        {
            var ctx = new SocketInteractionContext(Client, x);
            await _interactionService.ExecuteCommandAsync(ctx, serviceProvider);
        };

        Settings = await Settings.GetFromFileAsync();
        var discordToken = Settings.Token;

        Console.WriteLine("Запускаю бота...");
        await Client.LoginAsync(TokenType.Bot, discordToken);
        await Client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    public async Task CreateSlashCommandsAsync(ulong guildId)
    {
        await _interactionService.RegisterCommandsToGuildAsync(guildId);
    }

    public async Task ConnectToVoiceChannel(ulong channelId)
    {
        var channel = Client.GetGuild(Settings.GuildId).GetVoiceChannel(channelId);

        if (channel is null)
        {
            throw new StatusBasedException(400, $"Не удалось найти канал с id {channelId}");
        }
        
        try
        {
            AudioClient = await channel.ConnectAsync();
        }
        catch (Exception e)
        {
            if (LastTextChannel is not null)
                await LastTextChannel.SendMessageAsync("Error while connecting to a VC: " + e.Message);
            else
                throw;
        }
    }

    public event PlaybackChanged? PlaybackChanged;

    public async Task StopPlayback() => 
        await Task.Run(() => { PlaybackChanged?.Invoke(PlaybackChangeType.Stop, null); });
    public async Task PlayNewSong(int songIndex) =>
        await Task.Run(() => { PlaybackChanged?.Invoke(PlaybackChangeType.NewSong, songIndex); });
    public async Task NextPlayback() => 
        await Task.Run(() => { PlaybackChanged?.Invoke(PlaybackChangeType.Next, null); });
    public async Task PreviousPlayback() => 
        await Task.Run(() => { PlaybackChanged?.Invoke(PlaybackChangeType.Previous, null); });
    public async Task LeaveChannel() => 
        await Task.Run(() => { PlaybackChanged?.Invoke(PlaybackChangeType.Leave, null); });
    public async Task SetRepeat(bool repeat) => 
        await Task.Run(() => { PlaybackChanged?.Invoke(PlaybackChangeType.SetRepeat, null, repeat); });
}

public enum PlaybackChangeType
{
    Stop,
    Leave,
    NewSong,
    Next,
    Previous,
    SetRepeat
}

public delegate Task PlaybackChanged(PlaybackChangeType change, int? songIndex, bool repeat = false);