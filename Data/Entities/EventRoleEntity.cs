using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[PrimaryKey(nameof(EventId), nameof(UserId))]
[Table("EventRoles")]
public class EventRoleEntity
{
    [Required]
    [StringLength(450)]
    public string EventId { get; set; } = null!;

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = null!;

    public EventRoleType Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity User { get; set; } = null!;
}
