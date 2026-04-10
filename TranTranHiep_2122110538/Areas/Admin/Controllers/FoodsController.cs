using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
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

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, int? restaurantId = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);
        var query = _db.Foods.AsNoTracking().Include(f => f.Category).Include(f => f.Restaurant).AsQueryable();
        if (restaurantId.HasValue)
            query = query.Where(f => f.RestaurantId == restaurantId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Name)
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
                f.RestaurantId,
                RestaurantName = f.Restaurant!.Name,
                f.IsAvailable,
                f.StockQuantity
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var f = await _db.Foods.AsNoTracking().Include(x => x.Category).Include(x => x.Restaurant).FirstOrDefaultAsync(x => x.Id == id);
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
            f.RestaurantId,
            RestaurantName = f.Restaurant?.Name,
            f.IsAvailable,
            f.StockQuantity
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] FoodCreateEditRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = "Tên món bắt buộc." });

        if (model.RestaurantId is null or <= 0)
            return BadRequest(new { message = "Cần chọn RestaurantId (quán)." });

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.CategoryId);
        if (category == null || category.RestaurantId != model.RestaurantId.Value)
            return BadRequest(new { message = "Danh mục không thuộc quán đã chọn." });

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
            RestaurantId = model.RestaurantId.Value,
            CategoryId = model.CategoryId,
            IsAvailable = model.IsAvailable,
            StockQuantity = Math.Max(0, model.StockQuantity),
            Image = imagePath
        };
        _db.Foods.Add(food);
        await _db.SaveChangesAsync();
        return Ok(new { food.Id, message = "Đã tạo món.", food.Image });
    }

    [HttpPut]
    public async Task<IActionResult> Edit(int id, [FromForm] FoodCreateEditRequest model)
    {
        var food = await _db.Foods.FindAsync(id);
        if (food == null)
            return NotFound();

        if (model.RestaurantId is null or <= 0)
            return BadRequest(new { message = "Cần chọn RestaurantId (quán)." });

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.CategoryId);
        if (category == null || category.RestaurantId != model.RestaurantId.Value)
            return BadRequest(new { message = "Danh mục không thuộc quán đã chọn." });

        food.Name = model.Name.Trim();
        food.Price = model.Price;
        food.Description = model.Description;
        food.RestaurantId = model.RestaurantId.Value;
        food.CategoryId = model.CategoryId;
        food.IsAvailable = model.IsAvailable;
        food.StockQuantity = Math.Max(0, model.StockQuantity);

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
        var food = await _db.Foods.FindAsync(id);
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
