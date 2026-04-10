using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

/// <summary>Đánh giá món sau khi đơn hoàn thành (một lần / món / đơn).</summary>
public class FoodReview
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int FoodId { get; set; }
    public Food? Food { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
