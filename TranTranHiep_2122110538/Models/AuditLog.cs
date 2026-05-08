using System.ComponentModel.DataAnnotations;

namespace TranTranHiep_2122110538.Models;

public class AuditLog
{
    public int Id { get; set; }

    public int? ActorUserId { get; set; }
    public User? Actor { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [MaxLength(2000)]
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
