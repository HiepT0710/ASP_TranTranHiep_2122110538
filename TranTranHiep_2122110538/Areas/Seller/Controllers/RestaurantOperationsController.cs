using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.Services;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]/{id?}")]
public class RestaurantOperationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderAuditService _audit;

    public RestaurantOperationsController(AppDbContext db, IOrderAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private async Task<Restaurant?> GetMyRestaurantAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == userId);
    }

    [HttpGet]
    public async Task<IActionResult> Overview()
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var hours = await _db.RestaurantOperatingHours.AsNoTracking()
            .Where(x => x.RestaurantId == rest.Id)
            .OrderBy(x => x.DayOfWeek)
            .ToListAsync();

        return Ok(new
        {
            rest.Id,
            rest.Name,
            rest.Status,
            rest.StatusNote,
            rest.StatusUpdatedAt,
            rest.IsOpen,
            rest.IsAcceptingOrders,
            rest.OpeningHours,
            hours
        });
    }

    public class UpdateRestaurantStateRequest
    {
        public bool? IsOpen { get; set; }
        public bool? IsAcceptingOrders { get; set; }
        public string? OpeningHours { get; set; }
        public string? StatusNote { get; set; }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateState([FromBody] UpdateRestaurantStateRequest body)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var changes = new List<string>();
        if (body.IsOpen.HasValue) { rest.IsOpen = body.IsOpen.Value; changes.Add($"IsOpen={body.IsOpen.Value}"); }
        if (body.IsAcceptingOrders.HasValue) { rest.IsAcceptingOrders = body.IsAcceptingOrders.Value; changes.Add($"IsAcceptingOrders={body.IsAcceptingOrders.Value}"); }
        if (body.OpeningHours != null) { rest.OpeningHours = string.IsNullOrWhiteSpace(body.OpeningHours) ? null : body.OpeningHours.Trim(); changes.Add("OpeningHours"); }
        if (body.StatusNote != null) { rest.StatusNote = string.IsNullOrWhiteSpace(body.StatusNote) ? null : body.StatusNote.Trim(); changes.Add("StatusNote"); }
        rest.StatusUpdatedAt = DateTime.UtcNow;
        _audit.AddStatusChange(0, null, "RestaurantStateUpdated", null, Roles.Seller, string.Join(", ", changes));

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật trạng thái quán." });
    }

    public class UpsertOperatingHourRequest
    {
        public string DayOfWeek { get; set; } = string.Empty;
        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }
        public bool IsClosed { get; set; }
        public string? Note { get; set; }
    }

    [HttpPut]
    public async Task<IActionResult> UpsertHours([FromBody] UpsertOperatingHourRequest body)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var day = body.DayOfWeek.Trim();
        if (string.IsNullOrWhiteSpace(day))
            return BadRequest(new { message = "Thiếu ngày hoạt động." });

        var item = await _db.RestaurantOperatingHours.FirstOrDefaultAsync(x => x.RestaurantId == rest.Id && x.DayOfWeek == day);
        if (item == null)
        {
            item = new RestaurantOperatingHour { RestaurantId = rest.Id, DayOfWeek = day };
            _db.RestaurantOperatingHours.Add(item);
        }

        item.OpenTime = string.IsNullOrWhiteSpace(body.OpenTime) ? null : body.OpenTime.Trim();
        item.CloseTime = string.IsNullOrWhiteSpace(body.CloseTime) ? null : body.CloseTime.Trim();
        item.IsClosed = body.IsClosed;
        item.Note = string.IsNullOrWhiteSpace(body.Note) ? null : body.Note.Trim();
        _audit.AddStatusChange(0, null, "RestaurantHoursUpdated", null, Roles.Seller, $"{day}");
        await _db.SaveChangesAsync();

        return Ok(new { message = "Đã cập nhật giờ hoạt động." });
    }
}
