using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]/{id?}")]
public class RestaurantsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public RestaurantsController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private async Task<Restaurant?> GetMyRestaurantAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == userId);
    }

    [HttpGet]
    public async Task<IActionResult> My()
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        return Ok(new
        {
            rest.Id,
            rest.Name,
            rest.Address,
            rest.Phone,
            rest.CoverImage,
            rest.GalleryImage1,
            rest.GalleryImage2,
            rest.GalleryImage3,
            rest.IsOnSale,
            rest.SalePercent,
            rest.Status
        });
    }

    [HttpPut]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UpdateImages()
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var files = Request.Form.Files;
        string? cover = null, g1 = null, g2 = null, g3 = null;

        foreach (var file in files)
        {
            if (file.Length <= 0) continue;
            var saved = await SaveImageAsync(file);
            var key = file.Name.ToLowerInvariant();
            if (key == "coverimage") cover = saved;
            else if (key == "galleryimage1") g1 = saved;
            else if (key == "galleryimage2") g2 = saved;
            else if (key == "galleryimage3") g3 = saved;
        }

        if (Request.Form.TryGetValue("clearCoverImage", out var clearCover) && bool.TryParse(clearCover, out var clearCoverBool) && clearCoverBool)
            rest.CoverImage = null;
        if (Request.Form.TryGetValue("clearGalleryImage1", out var clearG1) && bool.TryParse(clearG1, out var clearG1Bool) && clearG1Bool)
            rest.GalleryImage1 = null;
        if (Request.Form.TryGetValue("clearGalleryImage2", out var clearG2) && bool.TryParse(clearG2, out var clearG2Bool) && clearG2Bool)
            rest.GalleryImage2 = null;
        if (Request.Form.TryGetValue("clearGalleryImage3", out var clearG3) && bool.TryParse(clearG3, out var clearG3Bool) && clearG3Bool)
            rest.GalleryImage3 = null;

        if (cover != null) rest.CoverImage = cover;
        if (g1 != null) rest.GalleryImage1 = g1;
        if (g2 != null) rest.GalleryImage2 = g2;
        if (g3 != null) rest.GalleryImage3 = g3;

        await _db.SaveChangesAsync();
        return Ok(new
        {
            message = "Đã cập nhật ảnh quán.",
            rest.Id,
            rest.CoverImage,
            rest.GalleryImage1,
            rest.GalleryImage2,
            rest.GalleryImage3
        });
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var folder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images", "restaurants");
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/images/restaurants/{fileName}";
    }
}
