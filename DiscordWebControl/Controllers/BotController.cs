using Discord.WebSocket;
using DiscordBot;
using DiscordBot.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DiscordWebControl.Controllers;

[ApiController]
public class BotController(Bot bot) : Controller
{
    private readonly ulong _guildId = bot.Settings?.GuildId ?? 0;

    [HttpPost("RegisterSlashCommands")]
    public async Task<IActionResult> RegisterSlashCommands()
    {
        await bot.CreateSlashCommandsAsync(_guildId);

        return NoContent();
    }

    [HttpGet("VoiceChannels")]
    [ProducesResponseType<IEnumerable<VoiceChannelSimpleDto>>(StatusCodes.Status200OK)]
    public IActionResult GetVoiceChannels()
    {
        return Ok(bot.Client.GetGuild(_guildId).VoiceChannels
            .Select(x => new VoiceChannelSimpleDto(x.Name, x.Id.ToString())));
    }
    
    [HttpGet("Songs")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public IActionResult GetSongs()
    {
        return Ok(PlaylistController.GetFileList());
    }
    
    [HttpGet("TrackStatus")]
    [ProducesResponseType<TrackStatus>(StatusCodes.Status200OK)]
    public IActionResult GetTrackStatus()
    {
        return Ok(bot.GetTrackStatus());
    }
    
    private async Task PlaySongByIndex(ulong? channelId, int songIndex, string notFoundErrorMessage)
    {
        if (channelId is null && bot.AudioClient is null && bot.AudioOutStream is null)
        {
            throw new StatusBasedException(400,
                "Дискорд бот сейчас не подключен ни к какому каналу, нужно передать channelId");
        }

        if (songIndex == -1)
        {
            throw new StatusBasedException(404, notFoundErrorMessage);
        }

        if (channelId is not null)
        {
            await bot.ConnectToVoiceChannel((ulong)channelId);
        }

        Task.Run(async () => { await bot.PlayNewSong(songIndex); });
    }
    
    [HttpPost("PlayByName")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PlayByName([FromQuery] string query, [FromQuery] ulong? channelId = null)
    {
        var index = PlaylistController.FindSongIndex(query, out var file);
        var notFoundErrorMessage = $"Не смог найти песню по запросу '{query}' :(";
        
        await PlaySongByIndex(channelId, index, notFoundErrorMessage);
        return NoContent();
    }

    [HttpPost("PlayByPrefix")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PlayByPrefix([FromQuery] int prefix, [FromQuery] ulong? channelId = null)
    {
        var index = PlaylistController.FindSongIndex(prefix, out var file);
        var notFoundErrorMessage = $"Не смог найти песню по префиксу '{prefix}' :(";
        
        await PlaySongByIndex(channelId, index, notFoundErrorMessage);
        return NoContent();
    }

    private record NewSongInfo(string SongName);

    [HttpPost("Next")]
    [ProducesResponseType<NewSongInfo>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Next()
    {
        if (bot.AudioClient is null)
        {
            throw new StatusBasedException(400, "Дискорд бот сейчас не подсоединён к каналу");
        }
        
        var songName = PlaylistController.GetNextSongName();
        if (songName is null)
        {
            throw new StatusBasedException(404, "Не удалось включить следующую песню");
        }
        
        Task.Run(async () => { await bot.NextPlayback(); });
        return Ok(new NewSongInfo(songName));
    }

    [HttpPost("Previous")]
    [ProducesResponseType<NewSongInfo>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Previous()
    {
        if (bot.AudioClient is null)
        {
            throw new StatusBasedException(400, "Дискорд бот сейчас не подсоединён к каналу");
        }
        
        var songName = PlaylistController.GetPreviousSongName();
        if (songName is null)
        {
            throw new StatusBasedException(404, "Не удалось включить предыдущую песню");
        }
        
        Task.Run(async () => { await bot.PreviousPlayback(); });
        return Ok(new NewSongInfo(songName));
    }

    [HttpPost("Stop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Stop()
    {
        if (bot.AudioClient is null && bot.AudioOutStream is null)
        {
            throw new StatusBasedException(400, "Дискорд бот сейчас ничего не играет");
        }

        await bot.StopPlayback();
        return NoContent();
    }

    [HttpPut("Repeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Repeat([FromQuery] bool repeat)
    {
        await bot.SetRepeat(repeat);
        return NoContent();
    }
}

public class VoiceChannelSimpleDto
{
    public VoiceChannelSimpleDto(string name, string id)
    {
        Name = name;
        Id = id;
    }

    public string Name { get; set; }
    public string Id { get; set; }
}