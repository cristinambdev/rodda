using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(EventId), nameof(CreatedAt))]
[Table("EventChatMessages")]
public class EventChatEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string EventId { get; set; } = null!;

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    public string? AuthorUserId { get; set; }

    [ForeignKey(nameof(AuthorUserId))]
    public virtual UserEntity? AuthorUser { get; set; }

    public string? AuthorDisplay { get; set; }

    [Required]
    public string Body { get; set; } = null!;
}
