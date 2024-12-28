using DiscordBot;
using Microsoft.AspNetCore.Mvc;

namespace DiscordWebControl.Controllers;

[ApiController]
public class BotController(Bot bot) : Controller
{
    [HttpPost("RegisterSlashCommands")]
    public async Task<IActionResult> RegisterSlashCommands()
    {
        const string guildId = "326813774507933697";
        await bot.CreateSlashCommandsAsync(ulong.Parse(guildId));

        return NoContent();
    }
    
    
}