using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranTranHiep_2122110538.Models;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = OrderStatuses.Pending;

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>Thời điểm hủy (UTC), có khi Status = Cancelled.</summary>
    public DateTime? CancelledAt { get; set; }

    [MaxLength(50)]
    public string? CancelledBy { get; set; }

    [MaxLength(500)]
    public string? CancelReason { get; set; }

    [Required, MaxLength(20)]
    public string PaymentMethod { get; set; } = PaymentMethods.COD;

    [Required, MaxLength(50)]
    public string PaymentStatus { get; set; } = PaymentStatuses.Pending;

    [Column(TypeName = "datetime2")]
    public DateTime? PaidAt { get; set; }

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [MaxLength(200)]
    public string? ShipperName { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? RefundedAt { get; set; }

    [MaxLength(500)]
    public string? RefundReason { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
    public ICollection<OrderPayment> Payments { get; set; } = new List<OrderPayment>();
    public ICollection<FoodReview> FoodReviews { get; set; } = new List<FoodReview>();
    public ICollection<OrderMessage> OrderMessages { get; set; } = new List<OrderMessage>();
}
