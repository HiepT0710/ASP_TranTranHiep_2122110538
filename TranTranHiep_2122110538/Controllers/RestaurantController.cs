using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Controllers;

/// <summary>Danh sách quán công khai (đã duyệt) — hỗ trợ trang đặt món.</summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]/[action]/{id?}")]
public class RestaurantController : Controller
{
    private readonly AppDbContext _db;

    public RestaurantController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? q = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var query = _db.Restaurants.AsNoTracking()
            .Where(r => r.Status == RestaurantStatuses.Approved);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r => r.Name.Contains(term) || (r.Address != null && r.Address.Contains(term)));
        }

        var total = await query.CountAsync();
        var pageList = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { r.Id, r.Name, r.Address, r.Phone, r.CoverImage, r.GalleryImage1, r.GalleryImage2, r.GalleryImage3, r.IsOnSale, r.SalePercent })
            .ToListAsync();

        var rids = pageList.Select(r => r.Id).ToList();
        var countRows = await _db.Foods.AsNoTracking()
            .Where(f => rids.Contains(f.RestaurantId) && f.IsAvailable)
            .GroupBy(f => f.RestaurantId)
            .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
            .ToListAsync();
        var countMap = countRows.ToDictionary(x => x.RestaurantId, x => x.Count);

        var items = pageList.Select(r => new
        {
            r.Id,
            r.Name,
            r.Address,
            r.Phone,
            r.CoverImage,
            r.GalleryImage1,
            r.GalleryImage2,
            r.GalleryImage3,
            r.IsOnSale,
            r.SalePercent,
            foodCount = countMap.GetValueOrDefault(r.Id)
        }).ToList();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet]
    public async Task<IActionResult> Sale()
    {
        var items = await _db.Restaurants.AsNoTracking()
            .Where(r => r.Status == RestaurantStatuses.Approved && r.IsOnSale)
            .OrderByDescending(r => r.SalePercent)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Address,
                r.Phone,
                r.CoverImage,
                r.GalleryImage1,
                r.GalleryImage2,
                r.GalleryImage3,
                r.IsOnSale,
                r.SalePercent,
                foodCount = r.Foods.Count(f => f.IsAvailable)
            })
            .Take(12)
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> BestSellers(int take = 8)
    {
        take = Math.Clamp(take, 1, 20);
        var items = await _db.Foods.AsNoTracking()
            .Where(f => f.IsAvailable && f.Restaurant!.Status == RestaurantStatuses.Approved)
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
                f.IsOnSale,
                f.SalePercent,
                OrdersCount = _db.OrderDetails.Count(od => od.FoodId == f.Id)
            })
            .OrderByDescending(x => x.OrdersCount)
            .ThenByDescending(x => x.IsOnSale)
            .Take(take)
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var r = await _db.Restaurants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == RestaurantStatuses.Approved);
        if (r == null)
            return NotFound(new { message = "Không tìm thấy quán hoặc quán chưa hoạt động." });

        var foodCount = await _db.Foods.CountAsync(f => f.RestaurantId == id && f.IsAvailable);
        var categoryCount = await _db.Categories.CountAsync(c => c.RestaurantId == id);

        return Ok(new
        {
            r.Id,
            r.Name,
            r.Address,
            r.Phone,
            r.CoverImage,
            r.GalleryImage1,
            r.GalleryImage2,
            r.GalleryImage3,
            r.IsOnSale,
            r.SalePercent,
            foodCount,
            categoryCount
        });
    }
}
