using Domain.Enums;

namespace Domain.Models;

public class EventAttendance
{
    public string Id { get; set; } = null!;
    public int GuestCount { get; set; }
    public DateTime RespondedAt { get; set; }
    public string EventId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public User? User { get; set; }
    public AttendanceStatus Status { get; set; }
    public bool HiddenFromList { get; set; }
}
