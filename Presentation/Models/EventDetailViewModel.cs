using Domain.Enums;
using Domain.Models;

namespace Presentation.Models;

public class EventDetailsViewModel
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Slug { get; set; }
    public string? CoverImageUrl { get; set; }
    public string CreatorDisplayLabel { get; set; } = "";
    public string? Description { get; set; }

    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string Timezone { get; set; } = null!;
    public JoinMode JoinMode { get; set; }
    public bool JoinEnabled => JoinMode != JoinMode.Disabled;
    public bool UserHasJoined { get; set; }
    public int UserGuestCount { get; set; } = 1;
    public int TotalJoinedGuests { get; set; }
    public EventStatus Status { get; set; }

    public bool ChatEnabled { get; set; }
    public bool ItemsTasksEnabled { get; set; }
    public bool AllowGuestBringItems { get; set; }
    public bool AllowGuestTasks { get; set; }

    public bool CanManageItemsTasks { get; set; }
    public bool CanAddItems { get; set; }
    public bool CanClaimItems { get; set; }
    public bool CanAddTasks { get; set; }
    public bool CanClaimTasks { get; set; }

    public Address? Location { get; set; }
    public PaymentDetails? Payment { get; set; }

    public List<EventItemViewModel> Items { get; set; } = new();
    public List<EventTaskViewModel> Tasks { get; set; } = new();
    public List<EventAttendeeViewModel> Attendees { get; set; } = new();
    public List<EventGuestRosterViewModel> HostGuestRoster { get; set; } = new();
    public IEnumerable<EventRole> EventRoles { get; set; } = new List<EventRole>();
    public List<ChatMessageViewModel> ChatMessages { get; set; } = new();
    public List<EventContributionsViewModel> OrganizerContributions { get; set; } = new();
    public int OrganizerContributionsTotalGuests { get; set; }
    public bool OrganizerContributionsShowFootnote { get; set; }
}
