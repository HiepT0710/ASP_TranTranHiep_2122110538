using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.ViewModels;

public class VoucherCreateRequest
{
    [Required]
    public int PromotionId { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public string? Note { get; set; }

    [Range(0, 100000000)]
    public decimal? MinOrderAmount { get; set; }

    [Range(0, 100000000)]
    public decimal? MaxDiscountAmount { get; set; }

    [Range(1, 100000)]
    public int UsageLimit { get; set; } = 1;

    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
}
