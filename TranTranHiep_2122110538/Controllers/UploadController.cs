using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class UploadController : Controller
{
    private readonly IWebHostEnvironment _env;

    public UploadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ReviewImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn ảnh hợp lệ." });

        var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "reviews");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var savePath = Path.Combine(uploadsDir, fileName);

        await using var stream = System.IO.File.Create(savePath);
        await file.CopyToAsync(stream);

        var url = $"/uploads/reviews/{fileName}";
        return Ok(new { url });
    }
}
