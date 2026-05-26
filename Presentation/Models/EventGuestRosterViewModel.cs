using Domain.Enums;

namespace Presentation.Models;

// One row in the host guests modal (roles + attendance merged)
public class EventGuestRosterViewModel
{
    public string UserId { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? RoleLabel { get; set; }
    public int GuestCount { get; set; } = 1;
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Pending;

    // Host may toggle co-owner for roster guests who are not the event owner.
    public bool CanToggleCoOwner { get; set; }

    public bool IsCoOwner { get; set; }
}
