namespace Presentation.Models;

/// Event row for list pages (All Events / My Events).
public class EventViewModel
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? ListTitle { get; set; }
    public string Hero { get; set; } = "";
    public string ListDateTime { get; set; } = "";
    public string ListLocation { get; set; } = "";
    public bool IsPast { get; set; }
    public bool HasCover { get; set; }
    public string DetailUrl { get; set; } = "";
    public string? RoleBadgeLabel { get; set; }
    public string AttendanceLabel { get; set; } = "";
    public bool AttendanceMuted { get; set; }
    public string AttendanceIconClass { get; set; } = "fa-solid fa-circle-info";
}
