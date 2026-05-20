using Domain.Enums;

namespace Presentation.Models;

public class EventItemViewModel
{
    public string Id { get; set; } = null!;
    public string EventId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Amount { get; set; }
    public int PeopleNeeded { get; set; } = 1;
    public SignupMode SignupMode { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? CreatedByDisplayName { get; set; }

    public List<EventAssignmentSlotViewModel> Assignments { get; set; } = new();
}
