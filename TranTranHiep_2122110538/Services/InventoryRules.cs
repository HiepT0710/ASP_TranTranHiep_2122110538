using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Services;

public static class InventoryRules
{
    /// <summary>Chỉ hoàn kho khi hủy từ Pending/Preparing (chưa giao).</summary>
    public static bool ShouldRestoreStockOnCancel(string oldStatus, string newStatus) =>
        newStatus == OrderStatuses.Cancelled
        && oldStatus != OrderStatuses.Cancelled
        && oldStatus != OrderStatuses.Completed
        && (oldStatus == OrderStatuses.Pending || oldStatus == OrderStatuses.Preparing);
}
