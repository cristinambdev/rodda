using Domain.Models;

namespace Presentation.Models;

public class EventsViewModel
{
    public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();
    public string UserId { get; set; } = "";
}
