using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[AllowAnonymous]
[Route("[controller]/[action]/{id?}")]
public class FoodController : Controller
{
    private readonly AppDbContext _db;

    public FoodController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Danh sách món từ quán đã duyệt — phân trang, lọc danh mục, tìm theo tên.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 8, int? categoryId = null, int? restaurantId = null, string? q = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 10);

        var query = _db.Foods.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Restaurant)
            .Where(f => f.IsAvailable && f.Restaurant!.Status == RestaurantStatuses.Approved);

        if (restaurantId.HasValue)
            query = query.Where(f => f.RestaurantId == restaurantId.Value);

        if (categoryId.HasValue)
            query = query.Where(f => f.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(f => f.Name.Contains(term));
        }

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
                f.IsAvailable
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            items
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var food = await _db.Foods.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Restaurant)
            .Where(f => f.Id == id && f.Restaurant!.Status == RestaurantStatuses.Approved)
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
                f.IsAvailable
            })
            .FirstOrDefaultAsync();

        if (food == null)
            return NotFound(new { message = "Không tìm thấy món." });

        return Ok(food);
    }
}
