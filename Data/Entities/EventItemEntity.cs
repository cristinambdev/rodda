using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

/// <summary>
/// Represents an item that guests can bring to an event.
/// Display fields (<see cref="Title"/>, <see cref="Amount"/>, <see cref="SignupMode"/>) may be edited by the assigned user.
/// <see cref="OwnerTitle"/> / <see cref="OwnerAmount"/> / <see cref="OwnerSignupMode"/> are updated only when an owner or co-owner saves
/// the row; when the assignee removes their assignment, copy owner fields back into the display fields.
/// </summary>
/// 
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

    [Required]
    [StringLength(300)]
    public string Title { get; set; } = null!;

    [StringLength(200)]
    public string? Amount { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public SignupMode SignupMode { get; set; } = SignupMode.Available;

    [Required]
    [StringLength(300)]
    public string OwnerTitle { get; set; } = null!;

    [StringLength(200)]
    public string? OwnerAmount { get; set; }

    public SignupMode OwnerSignupMode { get; set; } = SignupMode.Available;

    public string? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public virtual UserEntity? CreatedByUser { get; set; }

    public virtual ICollection<EventItemAssignmentEntity> Assignments { get; set; } = new List<EventItemAssignmentEntity>();
}
