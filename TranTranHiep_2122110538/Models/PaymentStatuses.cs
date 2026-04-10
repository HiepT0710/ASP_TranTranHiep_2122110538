namespace TranTranHiep_2122110538.Models;

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";

    public static readonly string[] All = { Pending, Paid, Failed, Refunded };
}
