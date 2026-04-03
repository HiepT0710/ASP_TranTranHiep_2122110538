namespace TranTranHiep_2122110538.Models;

/// <summary>Trạng thái duyệt quán (Admin).</summary>
public static class RestaurantStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";

    public static readonly string[] All = { Pending, Approved, Rejected, Suspended };
}
