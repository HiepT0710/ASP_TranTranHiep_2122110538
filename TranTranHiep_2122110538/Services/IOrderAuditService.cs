namespace TranTranHiep_2122110538.Services;

public interface IOrderAuditService
{
    void AddStatusChange(int orderId, string? fromStatus, string toStatus, int? actorUserId, string actorRole, string? note = null);
}
