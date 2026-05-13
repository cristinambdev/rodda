using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities;

[Index(nameof(EventItemId))]
[Table("EventItemAssignments")]
public class EventItemAssignmentEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [Column("ItemId")]
    public string EventItemId { get; set; } = null!;

    [ForeignKey(nameof(EventItemId))]
    public virtual EventItemEntity EventItem { get; set; } = null!;

    // Assignee details.
    public AssigneeType AssigneeType { get; set; }
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? User { get; set; }

    [StringLength(120)]
    public string? PlaceholderLabel { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
}
