using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }
    }
}
