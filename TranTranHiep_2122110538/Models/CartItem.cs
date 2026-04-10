using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

/// <summary>Giỏ hàng đăng nhập — đồng bộ đa thiết bị.</summary>
public class CartItem
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int FoodId { get; set; }
    public Food? Food { get; set; }

    [Range(1, 9999)]
    public int Quantity { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
