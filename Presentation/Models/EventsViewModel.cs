using Domain.Models;
using Presentation.Helpers;

namespace Presentation.Models;

public class EventsViewModel
{
    public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();
    public string UserId { get; set; } = "";
    public IReadOnlyDictionary<string, EventCardBadgeHelper.CardBadges> CardBadges { get; set; }
        = new Dictionary<string, EventCardBadgeHelper.CardBadges>();
}
