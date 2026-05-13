using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(TaskId))]
[Table("EventTaskAssignments")]
public class EventTaskAssignmentEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string TaskId { get; set; } = null!;

    [ForeignKey(nameof(TaskId))]
    public virtual EventTaskEntity Task { get; set; } = null!;

    public AssigneeType AssigneeType { get; set; }

    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? Assignee { get; set; }

    public string? PlaceholderLabel { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
}
