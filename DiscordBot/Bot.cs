using Discord;
using Discord.Audio;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot;

public class Bot(IServiceProvider serviceProvider)
{
    public IAudioClient? AudioClient { get; set; } = null;
    public AudioOutStream? AudioOutStream = null;
    public bool Repeat = false;
    
    public IMessageChannel? LastTextChannel { get; set; } = null;
    
    private readonly DiscordSocketClient _client = new();
    private InteractionService _interactionService = default!;
    private Settings _settings;
    
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    
    public void UpdateSettings(Settings discordSettings)
    {
        _settings = discordSettings;
    }

    public async Task StartAsync()
    {
        _client.Log += Log;
        // _client.Ready += ClientReady;
        // _client.SlashCommandExecuted += SlashCommandHandler;

        _interactionService = new InteractionService(_client.Rest);
        try
        {
            await _interactionService.AddModulesAsync(typeof(MainInteractionModule).Assembly, serviceProvider);
        }
        catch (Exception e)
        {
            Console.WriteLine("\n\nERROR WHILE ADDING MODULES: " + e.Message);
            throw;
        }

        _client.InteractionCreated += async (x) =>
        {
            var ctx = new SocketInteractionContext(_client, x);
            await _interactionService.ExecuteCommandAsync(ctx, serviceProvider);
        };

        _settings = await Settings.GetFromFileAsync();
        var discordToken = _settings.Token;

        Console.WriteLine("Запускаю бота...");
        await _client.LoginAsync(TokenType.Bot, discordToken);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }

    public async Task CreateSlashCommandsAsync(ulong guildId)
    {
        await _interactionService.RegisterCommandsToGuildAsync(guildId);
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
    public async Task ToggleRepeat() => 
        await Task.Run(() => { PlaybackChanged?.Invoke(PlaybackChangeType.ToggleRepeat, null); });
}

public enum PlaybackChangeType
{
    Stop,
    Leave,
    NewSong,
    Next,
    Previous,
    ToggleRepeat
}

public delegate Task PlaybackChanged(PlaybackChangeType change, int? songIndex);