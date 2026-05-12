using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(ItemId))]
[Table("EventItemAssignments")]
public class EventItemAssignmentEntity
{
    [Key]
    [StringLength(450)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(450)]
    public string ItemId { get; set; } = null!;

    [ForeignKey(nameof(ItemId))]
    public virtual EventItemEntity Item { get; set; } = null!;

    public AssigneeType AssigneeType { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? Assignee { get; set; }

    [StringLength(120)]
    public string? PlaceholderLabel { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
}
