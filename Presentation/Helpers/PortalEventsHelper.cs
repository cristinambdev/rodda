using Domain.Enums;
using Domain.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// PortalEventsHelper — slim JSON payload for client-side event list filtering (All Events / My Events / Home).
// ************************************************************************************************
// Consumers: `EventsController` (AllEventsList, MyEventsList), `HomeController`; `events-portal.js` reads
//            `ViewData["PortalEventsJson"]` to filter upcoming/past without full page reload.
// Card HTML is still server-rendered via `GetSingleEventCard`; this JSON is metadata only.
// ************************************************************************************************
public static class PortalEventsHelper
{
    // ************************************************************************************************
    // ToPortalPayload — one JSON object per event for the signed-in user.
    // What: Exposes id, creator flag, role, attendance status, date/time strings, upcoming vs past scope.
    // Why: FrontOffice list pages filter by date and role client-side; BackOffice reuses the same script pattern.
    // Related: `ToItem`; `events-portal.js` `renderLists` / filter tabs; respects `HiddenFromList` on roles/attendance.
    // Effects: Changing shape requires updating `events-portal.js`; does not affect server-rendered card badges.
    // ************************************************************************************************
    public static IReadOnlyList<object> ToPortalPayload(IEnumerable<Event> events, string userId)
    {
        var now = DateTimeOffset.UtcNow;
        return events.Select(e => ToItem(e, userId, now)).ToList();
    }

    // ************************************************************************************************
    // ToItem — builds a single anonymous object for one event in the portal JSON array.
    // What: Derives `myEventsRole`, `myAttendanceStatus`, ISO date, 24h time, and `timeScope` vs UTC now.
    // Why: Keeps payload small; cards already carry title/image from partials.
    // Related: `PortalEventsHelper` consumer scripts compare `timeScope` to show/hide list sections.
    // Effects: Wrong `timeScope` mis-files events between upcoming/past tabs until refresh.
    // ************************************************************************************************
    private static object ToItem(Event e, string userId, DateTimeOffset now)
    {
        var role = e.Roles.FirstOrDefault(r => r.UserId == userId && !r.HiddenFromList);
        var myAttendance = e.Attendances.FirstOrDefault(a => a.UserId == userId && !a.HiddenFromList);
        var isCreator = !string.IsNullOrEmpty(e.CreatedByUserId)
            && string.Equals(e.CreatedByUserId, userId, StringComparison.Ordinal);

        string? myEventsRole = role?.Role switch
        {
            EventRoleType.Owner => "Owner",
            EventRoleType.CoOwner => "CoOwner",
            _ => null
        };

        string? myAttendanceStatus = myAttendance?.Status switch
        {
            AttendanceStatus.Accepted => "Accepted",
            AttendanceStatus.Declined => "Declined",
            AttendanceStatus.Maybe => "Maybe",
            AttendanceStatus.Pending => "Pending",
            _ => null
        };

        var startUtc = e.StartAt.ToUniversalTime();

        return new
        {
            id = e.Id,
            creator = isCreator ? "you" : "",
            myEventsRole = myEventsRole ?? "",
            myAttendanceStatus = myAttendanceStatus ?? "",
            eventDateIso = startUtc.ToString("yyyy-MM-dd"),
            eventTime24 = startUtc.ToString("HH:mm"),
            timeScope = e.StartAt < now ? "past" : "upcoming"
        };
    }
}
