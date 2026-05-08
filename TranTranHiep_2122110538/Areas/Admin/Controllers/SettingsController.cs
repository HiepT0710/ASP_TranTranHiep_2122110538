using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("[area]/[controller]/[action]")]
public class SettingsController : Controller
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await _db.SystemSettings.AsNoTracking().OrderBy(x => x.Key).ToListAsync();
        return Ok(new { items = settings });
    }

    public class UpsertSettingRequest
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
    }

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertSettingRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Key)) return BadRequest(new { message = "Thiếu key." });
        var key = body.Key.Trim();
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
        if (setting == null)
        {
            setting = new SystemSetting { Key = key };
            _db.SystemSettings.Add(setting);
        }
        setting.Value = body.Value?.Trim();
        setting.Description = body.Description?.Trim();
        setting.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(setting);
    }
}
