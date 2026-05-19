using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

/// Represents a task that guests can complete at an event.
/// Display fields may be edited by the assigned user; owner fields hold the baseline for revert when the assignee leaves the slot

[Index(nameof(EventId))]
[Table("EventTasks")]
public class EventTaskEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string EventId { get; set; } = null!;

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    // CURRENT DISPLAY DATA (What the UI shows)
    // If an assignee edits the task, it changes THESE fields.
    [Required]
    [StringLength(300)]
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset? ScheduledAt { get; set; }

    [StringLength(32)]
    public string? TaskTime { get; set; }

    [StringLength(500)]
    public string? TaskLocation { get; set; }

    public int PeopleNeeded { get; set; } = 1;

    public SignupMode SignupMode { get; set; } = SignupMode.Available;

    // THE ORIGINAL BACKUP (Event creator data)
    // Used ONLY to revert the task if the assignee cancels.
    [Required]
    [StringLength(300)]
    public string OriginalTitle { get; set; } = string.Empty;

    public DateTimeOffset? OriginalScheduledAt { get; set; }
    [StringLength(32)]
    public string? OriginalTaskTime { get; set; }

    [StringLength(500)]
    public string? OriginalTaskLocation { get; set; }

    public int OriginalPeopleNeeded { get; set; } = 1;

    public SignupMode OriginalSignupMode { get; set; } = SignupMode.Available;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual UserEntity? CreatedByUser { get; set; }

    public virtual ICollection<EventTaskAssignmentEntity> Assignments { get; set; } = new List<EventTaskAssignmentEntity>();
}
