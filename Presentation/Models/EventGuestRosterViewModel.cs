using Domain.Enums;

namespace Presentation.Models;

/// <summary>One row in the host guests modal (roles + attendance merged).</summary>
public class EventGuestRosterViewModel
{
    public string UserId { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? RoleLabel { get; set; }
    public int GuestCount { get; set; } = 1;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Pending;
}
