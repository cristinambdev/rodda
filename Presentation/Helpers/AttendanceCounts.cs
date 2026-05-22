using Domain.Enums;
using Domain.Models;

namespace Presentation.Helpers;

/// <summary>Rolls up attendance rows for host-facing badges on cards and hero.</summary>
public static class AttendanceCounts
{
    public static (int Accepted, int Declined, int Pending) From(IEnumerable<EventAttendance> attendances)
    {
        var list = attendances.ToList();
        return (
            list.Count(a => a.Status == AttendanceStatus.Accepted),
            list.Count(a => a.Status == AttendanceStatus.Declined),
            list.Count(a => a.Status is AttendanceStatus.Pending or AttendanceStatus.Maybe));
    }
}
