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
public class ReportsController : Controller
{
    private readonly AppDbContext _db;
    public ReportsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(string? status = null)
    {
        var q = _db.ModerationReports.AsNoTracking().Include(x => x.Reporter).Include(x => x.Moderator).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        var items = await q.OrderByDescending(x => x.Id).Take(100).ToListAsync();
        return Ok(new { items });
    }

    public class ResolveRequest { public string Status { get; set; } = "Resolved"; public string? Note { get; set; } }

    [HttpPost]
    public async Task<IActionResult> Resolve(int id, [FromBody] ResolveRequest body)
    {
        var report = await _db.ModerationReports.FirstOrDefaultAsync(x => x.Id == id);
        if (report == null) return NotFound();
        report.Status = body.Status;
        report.UpdatedAt = DateTime.UtcNow;

        if (body.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
        {
            if (report.TargetType.Equals("FoodReview", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.FoodReviews.FirstOrDefaultAsync(x => x.Id == report.TargetId);
                if (item != null) { item.IsHidden = true; item.HiddenReason = report.Reason; }
            }
            else if (report.TargetType.Equals("RestaurantReview", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.RestaurantReviews.FirstOrDefaultAsync(x => x.Id == report.TargetId);
                if (item != null) { item.IsHidden = true; item.HiddenReason = report.Reason; }
            }
            else if (report.TargetType.Equals("Chat", StringComparison.OrdinalIgnoreCase))
            {
                var item = await _db.OrderMessages.FirstOrDefaultAsync(x => x.Id == report.TargetId);
                if (item != null) { item.IsHidden = true; item.HiddenReason = report.Reason; }
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật report.", report.Id, report.Status });
    }
}
