using Domain.Enums;
using Domain.Models;

namespace Presentation.Helpers;

/// <summary>Builds slim JSON for client-side list filtering; cards are rendered via <c>GetSingleEventCard</c>.</summary>
public static class PortalEventsHelper
{
    public static IReadOnlyList<object> ToPortalPayload(IEnumerable<Event> events, string userId)
    {
        var now = DateTimeOffset.UtcNow;
        return events.Select(e => ToItem(e, userId, now)).ToList();
    }

    private static object ToItem(Event e, string userId, DateTimeOffset now)
    {
        var role = e.Roles.FirstOrDefault(r => r.UserId == userId);
        var isCreator = !string.IsNullOrEmpty(e.CreatedByUserId)
            && string.Equals(e.CreatedByUserId, userId, StringComparison.Ordinal);

        string? myEventsRole = role?.Role switch
        {
            EventRoleType.Owner => "Owner",
            EventRoleType.CoOwner => "CoOwner",
            _ => null
        };

        var startUtc = e.StartAt.ToUniversalTime();

        return new
        {
            id = e.Id,
            creator = isCreator ? "you" : "",
            myEventsRole = myEventsRole ?? "",
            eventDateIso = startUtc.ToString("yyyy-MM-dd"),
            eventTime24 = startUtc.ToString("HH:mm"),
            timeScope = e.StartAt < now ? "past" : "upcoming"
        };
    }
}
