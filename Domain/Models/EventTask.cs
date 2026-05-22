using Domain.Enums;

namespace Domain.Models;
// Generated with help of AI
public class EventTask
{
    public string Id { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string EventId { get; set; } = null!;
    public string? EventTitle { get; set; }
    public string? EventSlug { get; set; }
    public DateTimeOffset? EventStartAt { get; set; }
    public string? EventListLocation { get; set; }

    // --- Current State ---
    public string Title { get; set; } = null!;
    public DateTimeOffset? ScheduledAt { get; set; }
    public string? TaskTime { get; set; }
    public string? TaskLocationName { get; set; }
    public Address? TaskLocation { get; set; }
    public int PeopleNeeded { get; set; } = 1;
    public SignupMode SignupMode { get; set; }

    // --- Original State ---
    public string OriginalTitle { get; set; } = null!;
    public DateTimeOffset? OrginalScheduledAt { get; set; }
    public string? OriginalTaskTime { get; set; }
    public string? OriginalTaskLocationName { get; set; }
    public Address? OriginalTaskLocation { get; set; }
    public int OriginalPeopleNeeded { get; set; } = 1;
    public SignupMode OriginalSignupMode { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    public string? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public List<EventTaskAssignment> Assignments { get; set; } = [];

}
