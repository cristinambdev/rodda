using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;


// Generated with help of AI
// Represents an item that guests can bring to an event.
// Display fields may be edited by the assigned user; owner fields hold the baseline for revert when the assignee leaves the slot

[Index(nameof(EventId))]
[Table("EventItems")]
public class EventItemEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string EventId { get; set; } = null!;

    [ForeignKey(nameof(EventId))]
    public virtual EventEntity Event { get; set; } = null!;

    // Current display data (What the UI shows)
    // If an assignee edits the item, it changes THESE fields.
    [Required]
    [StringLength(300)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string? Amount { get; set; }

    public int PeopleNeeded { get; set; } = 1;

    public SignupMode SignupMode { get; set; } = SignupMode.Available;

    // The original backup (Event creator data)
    // Used ONLY to revert the item if the assignee cancels.
    [Required]
    [StringLength(300)]
    public string OriginalTitle { get; set; } = null!;
    [StringLength(200)]
    public string? OriginalAmount { get; set; }

    public int OriginalPeopleNeeded { get; set; } = 1;

    public SignupMode OriginalSignupMode { get; set; } = SignupMode.Available;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual UserEntity? CreatedByUser { get; set; }

    public virtual ICollection<EventItemAssignmentEntity> Assignments { get; set; } = new List<EventItemAssignmentEntity>();
}
