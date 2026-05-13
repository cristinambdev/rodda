using Domain.Enums;

namespace Domain.Models;

public class EventItemAssignment
{
    public string Id { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string EventItemId { get; set; } = null!;
    public EventItem? EventItem { get; set; }
    public AssigneeType AssigneeType { get; set; }
    public string? UserId { get; set; }
    public User? User { get; set; }
    public string? PlaceholderLabel { get; set; }
    public AssignmentStatus Status { get; set; }
}
