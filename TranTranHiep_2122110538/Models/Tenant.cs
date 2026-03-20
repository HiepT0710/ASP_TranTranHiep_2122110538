using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models
{
    public class Tenant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [StringLength(20)]
        public string IdCard { get; set; } = string.Empty;
    }
}
