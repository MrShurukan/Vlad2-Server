namespace DiscordBot;

public class PlaylistController
{
    private readonly Bot _bot;

    public PlaylistController(Bot bot)
    {
        _bot = bot;
        
        _bot.PlaybackChanged += BotOnPlaybackChanged;
    }

    private void BotOnPlaybackChanged(PlaybackChangeType change, int? songIndex)
    {
        Console.WriteLine($"!{change.ToString()}: {songIndex}");
    }
}