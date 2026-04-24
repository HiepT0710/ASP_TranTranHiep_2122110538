using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

public class Promotion
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(20)]
    public string Scope { get; set; } = PromotionScopes.Food;

    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public int? FoodId { get; set; }
    public Food? Food { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime StartAt { get; set; } = DateTime.UtcNow;

    public DateTime EndAt { get; set; } = DateTime.UtcNow.AddDays(30);

    [Range(0, 100)]
    public int DiscountPercent { get; set; }

    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}

public static class PromotionScopes
{
    public const string Restaurant = "Restaurant";
    public const string Food = "Food";
}
