using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("[area]/[controller]/[action]/{id?}")]
public class RestaurantsController : Controller
{
    private readonly AppDbContext _db;

    public RestaurantsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 15, string? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.Restaurants.AsNoTracking().Include(r => r.Owner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Address,
                r.Phone,
                r.Status,
                r.OwnerId,
                OwnerUsername = r.Owner!.Username,
                CategoryCount = r.Categories.Count,
                FoodCount = r.Foods.Count
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await _db.Restaurants.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return NotFound();
        r.Status = RestaurantStatuses.Approved;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã duyệt quán.", r.Id, r.Status });
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id)
    {
        var r = await _db.Restaurants.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return NotFound();
        r.Status = RestaurantStatuses.Rejected;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã từ chối quán.", r.Id, r.Status });
    }

    [HttpPost]
    public async Task<IActionResult> Suspend(int id)
    {
        var r = await _db.Restaurants.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return NotFound();
        r.Status = RestaurantStatuses.Suspended;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã tạm ngưng quán.", r.Id, r.Status });
    }
}
