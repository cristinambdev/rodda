using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(TaskId))]
[Table("EventTaskAssignments")]
public class EventTaskAssignmentEntity
{
    [Key]
    [StringLength(450)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(450)]
    public string TaskId { get; set; } = null!;

    [ForeignKey(nameof(TaskId))]
    public virtual EventTaskEntity Task { get; set; } = null!;

    public AssigneeType AssigneeType { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? Assignee { get; set; }

    [StringLength(120)]
    public string? PlaceholderLabel { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
}
