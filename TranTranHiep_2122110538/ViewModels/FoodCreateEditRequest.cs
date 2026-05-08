using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TranTranHiep_2122110538.ViewModels;

public class FoodCreateEditRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100000000)]
    public decimal Price { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Admin gán quán khi tạo/sửa từ khu vực Admin.</summary>
    public int? RestaurantId { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsHidden { get; set; }

    [Range(0, 1000000)]
    public int StockQuantity { get; set; } = 100;

    [MaxLength(500)]
    public string? SaleScheduleNote { get; set; }

    public IFormFile? ImageFile { get; set; }
}
