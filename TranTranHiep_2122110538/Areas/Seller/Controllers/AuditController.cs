using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]")]
public class AuditController : Controller
{
    private readonly AppDbContext _db;

    public AuditController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? q = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = _db.AuditLogs.AsNoTracking().Include(x => x.Actor).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x =>
                x.Action.Contains(term) ||
                (x.EntityType != null && x.EntityType.Contains(term)) ||
                (x.Note != null && x.Note.Contains(term)) ||
                (x.Actor != null && x.Actor.Username.Contains(term)));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new { x.Id, x.Action, x.EntityType, x.EntityId, x.Note, x.MetadataJson, x.CreatedAt, Actor = x.Actor!.Username })
            .ToListAsync();
        return Ok(new { page, pageSize, total, items });
    }
}
