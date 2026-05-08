using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

public class Restaurant
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>User có Role = Seller (chủ quán).</summary>
    public int OwnerId { get; set; }
    public User? Owner { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? CoverImage { get; set; }

    [MaxLength(500)]
    public string? GalleryImage1 { get; set; }

    [MaxLength(500)]
    public string? GalleryImage2 { get; set; }

    [MaxLength(500)]
    public string? GalleryImage3 { get; set; }

    public bool IsOnSale { get; set; }

    public int SalePercent { get; set; }

    /// <summary>Quán đang mở để nhận đơn hay không.</summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>Tạm ngưng nhận đơn dù quán vẫn mở cửa (ví dụ quá tải).</summary>
    public bool IsAcceptingOrders { get; set; } = true;

    [MaxLength(500)]
    public string? OpeningHours { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = RestaurantStatuses.Pending;

    [MaxLength(500)]
    public string? StatusNote { get; set; }

    public DateTime? StatusUpdatedAt { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Food> Foods { get; set; } = new List<Food>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<RestaurantReview> Reviews { get; set; } = new List<RestaurantReview>();
}
