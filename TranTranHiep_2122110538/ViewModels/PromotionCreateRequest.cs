using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.ViewModels;

public class PromotionCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public string Scope { get; set; } = string.Empty;

    public int? RestaurantId { get; set; }
    public int? FoodId { get; set; }

    [Range(0, 100)]
    public int DiscountPercent { get; set; }

    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}
