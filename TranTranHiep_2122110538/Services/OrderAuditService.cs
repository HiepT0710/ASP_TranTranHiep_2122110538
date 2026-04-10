using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Services;

public class OrderAuditService : IOrderAuditService
{
    private readonly AppDbContext _db;

    public OrderAuditService(AppDbContext db)
    {
        _db = db;
    }

    public void AddStatusChange(int orderId, string? fromStatus, string toStatus, int? actorUserId, string actorRole, string? note = null)
    {
        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            FromStatus = string.IsNullOrEmpty(fromStatus) ? null : fromStatus,
            ToStatus = toStatus,
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            Note = note,
            CreatedAt = DateTime.UtcNow
        });
    }
}
