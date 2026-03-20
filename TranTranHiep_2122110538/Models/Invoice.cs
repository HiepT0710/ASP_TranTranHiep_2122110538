using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public int ContractId { get; set; }

        public DateTime Month { get; set; }

        public decimal RoomFee { get; set; }
        public decimal ElectricityFee { get; set; }
        public decimal WaterFee { get; set; }
        public decimal ServiceFee { get; set; }

        public decimal Total { get; set; }

        public string Status { get; set; } = "unpaid";

        [ForeignKey("ContractId")]
        public Contract? Contract { get; set; }
    }
}
