using Domain.Enums;

namespace Domain.Models;

public class Event
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string Timezone { get; set; } = null!;
    public JoinMode JoinMode { get; set; }
    public EventStatus Status { get; set; }

 
    public bool ChatEnabled { get; set; }
    public bool ItemsTasksEnabled { get; set; }
    public bool AllowGuestBringItems { get; set; }
    public bool AllowGuestTasks { get; set; }

    public Address? Location { get; set; }
    public PaymentDetails? Payment { get; set; }
}
