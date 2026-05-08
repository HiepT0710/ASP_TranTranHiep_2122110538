using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

public class RestaurantOperatingHour
{
    public int Id { get; set; }

    public int RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }

    [Required, MaxLength(16)]
    public string DayOfWeek { get; set; } = string.Empty;

    [MaxLength(5)]
    public string? OpenTime { get; set; }

    [MaxLength(5)]
    public string? CloseTime { get; set; }

    public bool IsClosed { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
