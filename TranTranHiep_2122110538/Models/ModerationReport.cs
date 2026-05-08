using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

public class ModerationReport
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string TargetType { get; set; } = string.Empty;

    public int TargetId { get; set; }

    [Required, MaxLength(100)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Detail { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "New";

    public int ReporterUserId { get; set; }
    public User? Reporter { get; set; }

    public int? ModeratorUserId { get; set; }
    public User? Moderator { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
