using Domain.Enums;

namespace Domain.Models;

public class EventRole
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public User? User { get; set; }
    public EventRoleType Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
