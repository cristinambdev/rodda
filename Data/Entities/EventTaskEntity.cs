using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

/// <summary>
/// Represents a task that guests can complete at an event.
/// Display fields may be edited by the assigned user; owner fields hold the baseline for revert when the assignee leaves the slot
/// (same pattern as <see cref="EventItemEntity"/>).
/// </summary>

[Index(nameof(EventId))]
[Table("EventTasks")]
public class EventTaskEntity
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

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    [StringLength(32)]
    public string? TaskTime { get; set; }

    [StringLength(500)]
    public string? TaskLocation { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public SignupMode SignupMode { get; set; } = SignupMode.Available;

    [Required]
    [StringLength(300)]
    public string OwnerTitle { get; set; } = string.Empty;

    [StringLength(32)]
    public string? OwnerTaskTime { get; set; }

    [StringLength(500)]
    public string? OwnerTaskLocation { get; set; }

    public SignupMode OwnerSignupMode { get; set; } = SignupMode.Available;

    [StringLength(450)]
    public string? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual UserEntity? CreatedByUser { get; set; }

    public virtual ICollection<EventTaskAssignmentEntity> Assignments { get; set; } = new List<EventTaskAssignmentEntity>();
}
