using Domain.Enums;
using Domain.Models;

namespace Presentation.Models;

public class EventDetailsViewModel
{
    // CORE EVENT DETAILS
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Slug { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string Timezone { get; set; } = null!;
    public JoinMode JoinMode { get; set; }
    public EventStatus Status { get; set; }

    // SETTINGS
    public bool ChatEnabled { get; set; }
    public bool ItemsTasksEnabled { get; set; }
    public bool AllowGuestBringItems { get; set; }
    public bool AllowGuestTasks { get; set; }

    public Address? Location { get; set; }
    public PaymentDetails? Payment { get; set; }

    // THE NESTED DATA (The missing pieces!)
    public List<EventItemViewModel> Items { get; set; } = new();
    public List<EventTaskViewModel> Tasks { get; set; } = new();
    public List<EventAttendeeViewModel> Attendees { get; set; } = new();


    public List<ChatMessageViewModel> ChatMessages { get; set; } = new();
}
