using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(Slug), IsUnique = true)]
[Table("Events")]
public class EventEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = null!;

    [StringLength(160)]
    public string? Slug { get; set; }

    [StringLength(2048)]
    public string? CoverImageUrl { get; set; }

    [StringLength(8000)]
    public string? Description { get; set; }

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }

    [Required]
    [StringLength(100)]
    public string Timezone { get; set; } = "Europe/Stockholm";

    [StringLength(300)]
    public string? LocationName { get; set; }

    [StringLength(500)]
    public string? LocationStreet { get; set; }

    [StringLength(32)]
    public string? LocationPostcode { get; set; }

    [StringLength(120)]
    public string? LocationCity { get; set; }

    [StringLength(100)]
    public string? LocationCountry { get; set; }

    public JoinMode JoinMode { get; set; } = JoinMode.Open;
    public EventStatus Status { get; set; } = EventStatus.Active;

    public bool ChatEnabled { get; set; }
    public bool? ItemsTasksEnabled { get; set; }
    public bool? AllowGuestBringItems { get; set; }
    public bool? AllowGuestTasks { get; set; }

    [StringLength(120)]
    public string? PaymentMethod { get; set; }

    [StringLength(120)]
    public string? PaymentNumber { get; set; }

    [StringLength(200)]
    public string? PaymentName { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal? PaymentAmount { get; set; }

    [StringLength(200)]
    public string? PaymentComment { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual UserEntity CreatedByUser { get; set; } = null!;

    public virtual ICollection<EventItemEntity> Items { get; set; } = new List<EventItemEntity>();
    public virtual ICollection<EventTaskEntity> Tasks { get; set; } = new List<EventTaskEntity>();
    public virtual ICollection<EventRoleEntity> Roles { get; set; } = new List<EventRoleEntity>();
    public virtual ICollection<EventAttendanceEntity> Attendances { get; set; } = new List<EventAttendanceEntity>();
    public virtual ICollection<EventChatEntity> ChatMessages { get; set; } = new List<EventChatEntity>();
}
