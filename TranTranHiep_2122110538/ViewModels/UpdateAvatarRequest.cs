using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TranTranHiep_2122110538.ViewModels;

public class UpdateAvatarRequest
{
    [Required]
    public IFormFile Avatar { get; set; } = default!;
}
