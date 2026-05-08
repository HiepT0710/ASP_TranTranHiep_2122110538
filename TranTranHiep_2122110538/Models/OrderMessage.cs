using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

/// <summary>Tin nhắn trao đổi theo đơn (khách ↔ quán / admin).</summary>
public class OrderMessage
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
