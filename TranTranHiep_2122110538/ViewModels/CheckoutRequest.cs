using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.ViewModels;

public class CheckoutPayload
{
    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? VoucherCode { get; set; }
}
