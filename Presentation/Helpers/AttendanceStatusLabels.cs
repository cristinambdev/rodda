using Domain.Enums;

namespace Presentation.Helpers;

// ************************************************************************************************
// AttendanceStatusLabels — maps `AttendanceStatus` to badge label text and CSS classes for the UI.
// ************************************************************************************************
// Consumers: `EventDetails.cshtml` hero, `_HorizontalEventCard`, `_VerticalEventCard`,
//            `_AttendanceSummaryBadges.cshtml` (host Accepted/Declined/Pending counts).
// Uses `AttendanceCounts.From` to supply counts; domain status comes from `EventAttendance` / join POST.
// Copy/CSS changes are presentation-only; RSVP logic lives in `EventsController` join/leave.
// ************************************************************************************************
public static class AttendanceStatusLabels
{
    // ************************************************************************************************
    // BadgeLabel — short English label for one attendance status.
    //  Accepted / Declined / Pending (Maybe maps to Pending label in summary partial).
    // Single source for consistent badge text across cards and event details.
    // Uses `BadgeCssClass` for pairing; `_AttendanceSummaryBadges` uses Accepted/Declined/Pending enums.
    // Changing labels updates all badges; does not change stored enum values.
    // ************************************************************************************************
    public static string BadgeLabel(AttendanceStatus status) =>
        status switch
        {
            AttendanceStatus.Accepted => "Accepted",
            AttendanceStatus.Declined => "Declined",
            _ => "Pending",
        };

    // ************************************************************************************************
    // BadgeCssClass — Bootstrap-style badge colour class per attendance status.
    // green / red / gray badge classes from `common.css` because host and guest see the same colour language for RSVP state.
    // Uses `BadgeLabel`; hero guest badge on `EventDetails` when not manager. CSS-only; class names must exist in stylesheet.
    // ************************************************************************************************
    public static string BadgeCssClass(AttendanceStatus status) =>
        status switch
        {
            AttendanceStatus.Accepted => "badge-green",
            AttendanceStatus.Declined => "badge-red",
            _ => "badge-gray",
        };

    // ************************************************************************************************
    // RoleBadgeCssClass — CSS class for Creator / Owner / Co-owner role pills.
    // Returns `badge-purple` for role badges on cards and event hero because role is separate from attendance status (you can be Owner and Declined).
    // Uses `EventDetails` `roleBadge` string; card partials `roleBadge` from roles. Visual only.
    // ************************************************************************************************
    public static string RoleBadgeCssClass => "badge-purple";
}
