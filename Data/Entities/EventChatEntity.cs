using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(EventId), nameof(CreatedAt))]
[Table("EventChatMessages")]
public class EventChatEntity
{
    [Key]
    [StringLength(450)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(450)]
    public string EventId { get; set; } = null!;

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    [StringLength(450)]
    public string? AuthorUserId { get; set; }

    [ForeignKey(nameof(AuthorUserId))]
    public virtual UserEntity? AuthorUser { get; set; }

    [StringLength(200)]
    public string? AuthorDisplay { get; set; }

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = null!;
}
