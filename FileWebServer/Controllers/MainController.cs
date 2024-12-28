using Microsoft.AspNetCore.Mvc;

namespace FileWebServer.Controllers;

[ApiController()]
[Route("Main")]
public class MainController(IWebHostEnvironment webHostEnvironment) 
    : Controller
{
    [HttpPost("Files")]
    public async Task<IActionResult> PostFiles(List<IFormFile> files)
    {
        var uploadPath = Path.Combine(webHostEnvironment.ContentRootPath, "uploads");

        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        foreach (var file in files)
        {
            var filePath = Path.Combine(uploadPath, file.FileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }

        return Ok("Files saved");
    }
    
    [HttpPost("File")]
    public async Task<IActionResult> PostFile(IFormFile file)
    {
        var uploadPath = Path.Combine(webHostEnvironment.ContentRootPath, "uploads");

        if (!Directory.Exists(uploadPath))
            Directory.CreateDirectory(uploadPath);

        var filePath = Path.Combine(uploadPath, file.FileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok("File saved");
    }
}