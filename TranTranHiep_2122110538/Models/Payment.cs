using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; } = "cash";

        public DateTime PaymentDate { get; set; }

        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; }
    }
}
