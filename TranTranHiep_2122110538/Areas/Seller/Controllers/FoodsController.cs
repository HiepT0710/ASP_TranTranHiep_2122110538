using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]/{id?}")]
public class FoodsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public FoodsController(AppDbContext db, IWebHostEnvironment env)
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
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var query = _db.Foods.AsNoTracking().Include(f => f.Category)
            .Where(f => f.RestaurantId == rest.Id)
            .OrderBy(f => f.Name);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Price,
                f.Image,
                f.Description,
                f.CategoryId,
                CategoryName = f.Category!.Name,
                f.IsAvailable
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var f = await _db.Foods.AsNoTracking().Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id && x.RestaurantId == rest.Id);
        if (f == null)
            return NotFound();
        return Ok(new
        {
            f.Id,
            f.Name,
            f.Price,
            f.Image,
            f.Description,
            f.CategoryId,
            CategoryName = f.Category?.Name,
            f.IsAvailable
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] FoodCreateEditRequest model)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = "Tên món bắt buộc." });

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.CategoryId && c.RestaurantId == rest.Id);
        if (category == null)
            return BadRequest(new { message = "Danh mục không thuộc quán của bạn." });

        var imagePath = "/images/foods/placeholder.svg";
        if (model.ImageFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(model.ImageFile.FileName);
            if (string.IsNullOrEmpty(ext) || ext.Length > 10)
                ext = ".jpg";
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var dir = Path.Combine(_env.WebRootPath, "images", "foods");
            Directory.CreateDirectory(dir);
            var full = Path.Combine(dir, fileName);
            await using (var stream = System.IO.File.Create(full))
                await model.ImageFile.CopyToAsync(stream);
            imagePath = $"/images/foods/{fileName}";
        }

        var food = new Food
        {
            Name = model.Name.Trim(),
            Price = model.Price,
            Description = model.Description,
            RestaurantId = rest.Id,
            CategoryId = model.CategoryId,
            IsAvailable = model.IsAvailable,
            Image = imagePath
        };
        _db.Foods.Add(food);
        await _db.SaveChangesAsync();
        return Ok(new { food.Id, message = "Đã tạo món.", food.Image });
    }

    [HttpPut]
    public async Task<IActionResult> Edit(int id, [FromForm] FoodCreateEditRequest model)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == id && f.RestaurantId == rest.Id);
        if (food == null)
            return NotFound();

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.CategoryId && c.RestaurantId == rest.Id);
        if (category == null)
            return BadRequest(new { message = "Danh mục không thuộc quán của bạn." });

        food.Name = model.Name.Trim();
        food.Price = model.Price;
        food.Description = model.Description;
        food.CategoryId = model.CategoryId;
        food.IsAvailable = model.IsAvailable;

        if (model.ImageFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(model.ImageFile.FileName);
            if (string.IsNullOrEmpty(ext) || ext.Length > 10)
                ext = ".jpg";
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var dir = Path.Combine(_env.WebRootPath, "images", "foods");
            Directory.CreateDirectory(dir);
            var full = Path.Combine(dir, fileName);
            await using (var stream = System.IO.File.Create(full))
                await model.ImageFile.CopyToAsync(stream);

            if (!string.IsNullOrEmpty(food.Image) && food.Image.StartsWith("/images/foods/", StringComparison.OrdinalIgnoreCase))
            {
                var old = Path.Combine(_env.WebRootPath, food.Image.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(old) && !old.EndsWith("placeholder.svg", StringComparison.OrdinalIgnoreCase))
                    System.IO.File.Delete(old);
            }

            food.Image = $"/images/foods/{fileName}";
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật.", food.Image });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == id && f.RestaurantId == rest.Id);
        if (food == null)
            return NotFound();

        if (await _db.OrderDetails.AnyAsync(od => od.FoodId == id))
            return BadRequest(new { message = "Không xóa được: món đã xuất hiện trong đơn hàng." });

        if (!string.IsNullOrEmpty(food.Image) && food.Image.StartsWith("/images/foods/", StringComparison.OrdinalIgnoreCase))
        {
            var path = Path.Combine(_env.WebRootPath, food.Image.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(path) && !path.EndsWith("placeholder.svg", StringComparison.OrdinalIgnoreCase))
                System.IO.File.Delete(path);
        }

        _db.Foods.Remove(food);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa." });
    }
}
