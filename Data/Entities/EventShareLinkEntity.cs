using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(Token), IsUnique = true)]
[Table("EventShareLinks")]
public class EventShareLinkEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string EventId { get; set; } = null!;

    [Required]
    [StringLength(64)]
    public string Token { get; set; } = Guid.NewGuid().ToString("N");

    public bool IsRevoked { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;
}
