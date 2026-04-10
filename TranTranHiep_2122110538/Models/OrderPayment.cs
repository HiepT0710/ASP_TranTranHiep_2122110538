using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranTranHiep_2122110538.Models;

/// <summary>Giao dịch thanh toán / hoàn tiền (demo cổng VNPay/MoMo qua mã giả lập).</summary>
public class OrderPayment
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(20)]
    public string Kind { get; set; } = PaymentKinds.Online;

    [Required, MaxLength(20)]
    public string Method { get; set; } = PaymentMethods.COD;

    [Required, MaxLength(50)]
    public string Status { get; set; } = PaymentStatuses.Paid;

    [MaxLength(200)]
    public string? ExternalTransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Note { get; set; }
}
