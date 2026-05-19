using Domain.Enums;

namespace Presentation.Models;

public class EventAttendeeViewModel
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string? DisplayName { get; set; }
    public int GuestCount { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime RespondedAt { get; set; }
}
