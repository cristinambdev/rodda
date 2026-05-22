using Domain.Enums;

namespace Presentation.Helpers;

/// <summary>Maps attendance enums to host-facing badge copy on event details.</summary>
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
            AttendanceStatus.Accepted => "event-attendance-badge--accepted",
            AttendanceStatus.Declined => "event-attendance-badge--declined",
            _ => "event-attendance-badge--pending",
        };
}
