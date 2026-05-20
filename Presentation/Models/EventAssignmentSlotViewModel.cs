using Domain.Enums;

namespace Presentation.Models;

public class EventAssignmentSlotViewModel
{
    public string Id { get; set; } = null!;
    public AssigneeType AssigneeType { get; set; }
    public string? UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? PlaceholderLabel { get; set; }
    public AssignmentStatus Status { get; set; }
}
