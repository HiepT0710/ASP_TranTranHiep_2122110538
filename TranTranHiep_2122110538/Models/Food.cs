using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TranTranHiep_2122110538.Models;

public class Food
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string? Image { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public bool IsAvailable { get; set; } = true;

    /// <summary>Số lượng tồn kho (món còn bán được).</summary>
    public int StockQuantity { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<FoodReview> Reviews { get; set; } = new List<FoodReview>();
}
