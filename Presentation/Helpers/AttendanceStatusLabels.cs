using Domain.Enums;

namespace Presentation.Helpers;

// ************************************************************************************************
// Maps attendance enums to host-facing badge copy on event details.
public static class AttendanceStatusLabels
{
    public static string BadgeLabel(AttendanceStatus status) =>
        status switch
        {
            AttendanceStatus.Accepted => "Accepted",
            AttendanceStatus.Declined => "Declined",
            _ => "Pending",
        };

    public static string BadgeCssClass(AttendanceStatus status) =>
        status switch
        {
            AttendanceStatus.Accepted => "badge-green",
            AttendanceStatus.Declined => "badge-red",
            _ => "badge-gray",
        };

    public static string RoleBadgeCssClass => "badge-purple";
}
