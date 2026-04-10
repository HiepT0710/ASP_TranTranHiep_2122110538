using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

/// <summary>Đăng ký Web Push (VAPID) của trình duyệt.</summary>
public class PushSubscription
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, MaxLength(2048)]
    public string Endpoint { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string P256dh { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
