using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranTranHiep_2122110538.Models;

[Table("Users")]
public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>Lưu mật khẩu đã băm (ASP.NET Identity PasswordHasher).</summary>
    [Required, MaxLength(500)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? FullName { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Required, MaxLength(50)]
    public string Role { get; set; } = "User";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    /// <summary>1 Seller — 1 quán (nếu Role = Seller).</summary>
    public Restaurant? OwnedRestaurant { get; set; }

    public ICollection<OrderStatusHistory> OrderStatusHistoriesAsActor { get; set; } = new List<OrderStatusHistory>();
    public ICollection<FoodReview> FoodReviews { get; set; } = new List<FoodReview>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<PushSubscription> PushSubscriptions { get; set; } = new List<PushSubscription>();
}
