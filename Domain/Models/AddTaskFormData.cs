using Domain.Enums;

namespace Domain.Models;

public class AddTaskFormData
{
    public string EventId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DateTimeOffset? ScheduledAt { get; set; }
    public string? TaskTime { get; set; }
    public string? TaskLocation { get; set; }
    public SignupMode SignupMode { get; set; } = SignupMode.Available;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
