using Microsoft.AspNetCore.Mvc;

namespace DiscordWebControl.Controllers;

[ApiController]
[Route("Test")]
public class TestController : Controller
{
    [HttpGet("Test")]
    public IActionResult Test()
    {
        return Ok("Test");
    }
}