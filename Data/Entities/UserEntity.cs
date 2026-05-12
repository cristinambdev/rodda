using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Data.Entities;

public class UserEntity : IdentityUser
{
    [Required]
    [StringLength(256)]
    public string DisplayName { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // What the user created (Ownership)
    public virtual ICollection<EventEntity> CreatedEvents { get; set; } = new List<EventEntity>();
    public virtual ICollection<EventItemEntity> CreatedEventItems { get; set; } = new List<EventItemEntity>();
    public virtual ICollection<EventTaskEntity> CreatedEventTasks { get; set; } = new List<EventTaskEntity>();
    public virtual ICollection<EventChatEntity> CreatedChatMessages { get; set; } = new List<EventChatEntity>();

    // How the user participates (Participation)
    public virtual ICollection<EventRoleEntity> EventRoles { get; set; } = new List<EventRoleEntity>();
    public virtual ICollection<EventAttendanceEntity> EventAttendances { get; set; } = new List<EventAttendanceEntity>();
    public virtual ICollection<EventItemAssignmentEntity> EventItemAssignments { get; set; } = new List<EventItemAssignmentEntity>();
    public virtual ICollection<EventTaskAssignmentEntity> EventTaskAssignments { get; set; } = new List<EventTaskAssignmentEntity>();
}
