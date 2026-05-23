namespace Presentation.Models;

public class EventContributionsViewModel
{
    public string PersonName { get; set; } = "";
    public int PlusGuests { get; set; }
    public bool IsSelf { get; set; }
    public bool IsPlaceholderPerson { get; set; }
    public List<EventContributionsViewModel> Items { get; set; } = new();
    public List<EventContributionsViewModel> Tasks { get; set; } = new();
    public string Text { get; set; } = "";
    public bool IsEveryoneLine { get; set; }
    public bool IsDone { get; set; }
}
