namespace TranTranHiep_2122110538.Models;

public static class InventoryRules
{
    /// <summary>
    /// Hoàn kho khi đơn bị hủy trước khi bắt đầu xử lý thực tế.
    /// Với luồng hiện tại, chỉ cần đơn chưa đi xa hơn Pending/Preparing thì có thể trả kho.
    /// </summary>
    public static bool ShouldRestoreStockOnCancel(string fromStatus, string toStatus)
    {
        if (toStatus != OrderStatuses.Cancelled)
            return false;

        return fromStatus == OrderStatuses.Pending || fromStatus == OrderStatuses.Preparing;
    }
}
