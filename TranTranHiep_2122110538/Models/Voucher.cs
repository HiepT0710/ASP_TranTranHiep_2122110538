using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

public class Voucher
{
    public int Id { get; set; }

    public int PromotionId { get; set; }
    public Promotion? Promotion { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, 100000000)]
    public decimal? MinOrderAmount { get; set; }

    [Range(0, 100000000)]
    public decimal? MaxDiscountAmount { get; set; }

    public int UsageLimit { get; set; } = 1;

    public int UsedCount { get; set; }

    /// <summary>Số lần dùng tối đa cho mỗi người dùng.</summary>
    [Range(1, 100)]
    public int PerUserLimit { get; set; } = 1;

    public DateTime StartAt { get; set; } = DateTime.UtcNow;

    public DateTime EndAt { get; set; } = DateTime.UtcNow.AddDays(30);
}
