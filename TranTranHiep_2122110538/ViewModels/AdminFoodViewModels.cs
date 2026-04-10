using Microsoft.AspNetCore.Http;

namespace TranTranHiep_2122110538.ViewModels;

public class FoodCreateEditRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }

    /// <summary>Admin gán quán khi tạo/sửa từ khu vực Admin.</summary>
    public int? RestaurantId { get; set; }

    public int CategoryId { get; set; }
    public bool IsAvailable { get; set; } = true;

    /// <summary>Tồn kho bán được.</summary>
    public int StockQuantity { get; set; } = 100;

    public IFormFile? ImageFile { get; set; }
}
