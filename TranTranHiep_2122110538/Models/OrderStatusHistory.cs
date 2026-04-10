using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

/// <summary>Lịch sử thay đổi trạng thái đơn (audit).</summary>
public class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [MaxLength(50)]
    public string? FromStatus { get; set; }

    [Required, MaxLength(50)]
    public string ToStatus { get; set; } = string.Empty;

    public int? ActorUserId { get; set; }
    public User? Actor { get; set; }

    [Required, MaxLength(50)]
    public string ActorRole { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
