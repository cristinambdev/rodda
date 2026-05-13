using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Table("EventRoles")]
public class EventRoleEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required]
    public string EventId { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = null!;

    public EventRoleType Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity User { get; set; } = null!;
}
