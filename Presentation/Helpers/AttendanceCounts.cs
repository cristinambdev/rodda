using Domain.Enums;
using Domain.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// AttendanceCounts — rolls up attendance rows into Accepted / Declined / Pending counts for host badges.
// ************************************************************************************************
// Consumers: `_HorizontalEventCard`, `_VerticalEventCard`, `EventDetails` hero (`HostGuestRoster` tuple),
//            `_AttendanceSummaryBadges.cshtml` via `attendanceCounts` model.
// Related: `AttendanceStatusLabels` for how each bucket is rendered; join/leave updates underlying rows.
// Count logic changes host summary pills only; guest modal uses full roster from `BuildHostGuestRoster`.
// ************************************************************************************************
public static class AttendanceCounts
{
    // ************************************************************************************************
    // From — counts attendance rows by status group.
    // Returns tuple (Accepted, Declined, Pending+Maybe) because host hero and cards show aggregate RSVP summary
    // without listing every guest. `EventDetails` builds tuple from `HostGuestRoster`; cards use `Model.Attendances`.
    // Maybe is grouped with Pending in the third pill (matches `_AttendanceSummaryBadges`).
    // ************************************************************************************************
    public static (int Accepted, int Declined, int Pending) From(IEnumerable<EventAttendance> attendances)
    {
        var list = attendances.ToList();
        return (
            list.Count(a => a.Status == AttendanceStatus.Accepted),
            list.Count(a => a.Status == AttendanceStatus.Declined),
            list.Count(a => a.Status is AttendanceStatus.Pending or AttendanceStatus.Maybe));
    }
}
