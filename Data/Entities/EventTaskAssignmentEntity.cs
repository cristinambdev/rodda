using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Data.Entities;

[Index(nameof(EventTaskId))]
[Table("EventTaskAssignments")]
public class EventTaskAssignmentEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string EventTaskId { get; set; } = null!;

    [ForeignKey(nameof(EventTaskId))]
    public virtual EventTaskEntity EventTask { get; set; } = null!;

    public AssigneeType AssigneeType { get; set; }

    public string? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual UserEntity? User { get; set; }

    public string? PlaceholderLabel { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
}
