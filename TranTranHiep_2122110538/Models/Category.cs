using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    public ICollection<Food> Foods { get; set; } = new List<Food>();
}
