namespace TranTranHiep_2122110538.Models;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Preparing = "Preparing";
    public const string Delivering = "Delivering";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Pending, Preparing, Delivering, Completed, Cancelled };

    /// <summary>Trạng thái Seller được phép gán khi cập nhật đơn của quán mình.</summary>
    public static readonly string[] SellerAssignable = { Preparing, Delivering, Completed };

    /// <summary>Khách được phép hủy: chỉ khi đơn còn Pending (quán chưa chuyển sang Đang chuẩn bị).</summary>
    public static readonly string[] CustomerCancellable = { Pending };
}
