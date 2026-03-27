using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models
{
    public class MeterReading
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ContractId { get; set; }

        public int ElectricityOld { get; set; }
        public int ElectricityNew { get; set; }

        public int WaterOld { get; set; }
        public int WaterNew { get; set; }

        [ForeignKey("ContractId")]
        public Contract? Contract { get; set; }
    }
}
