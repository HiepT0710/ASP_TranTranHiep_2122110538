using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

/// <summary>Đánh giá quán sau khi đơn hoàn thành.</summary>
public class RestaurantReview
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    [MaxLength(4000)]
    public string? ImageUrlsJson { get; set; }

    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
