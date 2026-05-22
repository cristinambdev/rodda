using System.Globalization;
using Domain.Enums;
using Domain.Models;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// Builds the server-rendered home “My To-Dos” view model from items/tasks assigned to the viewer.
public static class HomeTodosHelper
{
    // ************************************************************************************************
    public static HomeIndexViewModel Build(
        IEnumerable<EventItem> items,
        IEnumerable<EventTask> tasks,
        string userId,
        string? rangeRaw)
    {
        var range = NormalizeRange(rangeRaw);
        var allEvents = BuildEventCards(items, tasks, userId);
        var filtered = new List<HomeTodoEventCardViewModel>();
        var index = 0;
        foreach (var card in allEvents.Where(e => IsInRange(e.StartAt, range)).OrderBy(e => e.StartAt))
        {
            card.CardTheme = index % 2 == 0 ? "teal" : "tan";
            filtered.Add(card);
            index++;
        }

        return new HomeIndexViewModel
        {
            ActiveRange = range,
            TodoEvents = filtered,
            EmptyMessage = EmptyMessageForRange(range),
        };
    }

    // ************************************************************************************************
    private static List<HomeTodoEventCardViewModel> BuildEventCards(
        IEnumerable<EventItem> items,
        IEnumerable<EventTask> tasks,
        string userId)
    {
        // Service query already scopes rows; assignments must be mapped on the domain model (see MapItemForHomeTodos).
        var itemList = items.Where(i => RowVisibleToUser(i.Assignments, userId)).ToList();
        var taskList = tasks.Where(t => RowVisibleToUser(t.Assignments, userId)).ToList();

        var eventIds = itemList.Select(i => i.EventId)
            .Concat(taskList.Select(t => t.EventId))
            .Distinct(StringComparer.Ordinal);

        var cards = new List<HomeTodoEventCardViewModel>();

        foreach (var eventId in eventIds)
        {
            var eventItems = itemList.Where(i => i.EventId == eventId).ToList();
            var eventTasks = taskList.Where(t => t.EventId == eventId).ToList();
            var metaItem = eventItems.FirstOrDefault();
            var metaTask = eventTasks.FirstOrDefault();

            var title = metaItem?.EventTitle ?? metaTask?.EventTitle ?? "";
            var slug = metaItem?.EventSlug ?? metaTask?.EventSlug;
            var startAt = metaItem?.EventStartAt ?? metaTask?.EventStartAt ?? DateTimeOffset.UtcNow;
            var listLocation = metaItem?.EventListLocation ?? metaTask?.EventListLocation ?? "";

            var startLocal = startAt.ToLocalTime();
            var listDateTime = startLocal.ToString("ddd, MMM d · h:mm tt", CultureInfo.InvariantCulture);
            var raw = listDateTime.Split('·', StringSplitOptions.TrimEntries);
            var datePart = raw.Length > 0 ? raw[0] : "";
            var timePart = raw.Length > 1 ? raw[1] : startLocal.ToString("HH:mm");

            var bringLines = eventItems.Select(i => new HomeTodoBringLineViewModel
            {
                Title = FormatLineTitle(i.Title),
                Amount = FormatAmount(i.Amount),
                Done = IsDoneOnServer(i.Assignments, userId),
            }).ToList();

            var taskLines = eventTasks.Select(t => new HomeTodoTaskLineViewModel
            {
                Title = FormatLineTitle(t.Title),
                TaskTime = (t.TaskTime ?? "").Trim(),
                TaskLocation = (t.TaskLocationName ?? "").Trim(),
                Done = IsDoneOnServer(t.Assignments, userId),
            }).ToList();

            if (bringLines.Count == 0 && taskLines.Count == 0)
                continue;

            var doneCount = bringLines.Count(l => l.Done) + taskLines.Count(l => l.Done);
            var lineCount = bringLines.Count + taskLines.Count;

            cards.Add(new HomeTodoEventCardViewModel
            {
                EventId = eventId,
                Title = title,
                DetailUrl = EventUrls.DetailsPath(slug, eventId),
                ListDateTime = listDateTime,
                DatePart = datePart,
                TimePart = timePart,
                ListLocation = listLocation,
                StartAt = startAt,
                DoneCount = doneCount,
                LineCount = lineCount,
                BringItems = bringLines,
                GuestTasks = taskLines,
            });
        }

        return cards.OrderBy(c => c.StartAt).ToList();
    }

    // ************************************************************************************************
    private static string NormalizeRange(string? range) =>
        range switch
        {
            "week" => "week",
            "year" => "year",
            _ => "month",
        };

    // ************************************************************************************************
    private static string EmptyMessageForRange(string range) =>
        range switch
        {
            "week" => "Nothing on your plate this week.",
            "year" => "Nothing on your plate this year.",
            _ => "Nothing on your plate this month.",
        };

    // ************************************************************************************************
    private static bool IsInRange(DateTimeOffset startAt, string range)
    {
        var now = DateTime.Now;
        var eventLocal = startAt.ToLocalTime();
        var startOfToday = new DateTime(now.Year, now.Month, now.Day);
        if (eventLocal < startOfToday)
            return false;

        return range switch
        {
            "week" => eventLocal < startOfToday.AddDays(7),
            "month" => eventLocal <= new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59),
            "year" => eventLocal <= new DateTime(now.Year, 12, 31, 23, 59, 59),
            _ => true,
        };
    }

    // ************************************************************************************************
    private static string FormatLineTitle(string title)
    {
        var s = (title ?? "").Trim();
        if (s.Length == 0) return "";
        return char.ToUpper(s[0]) + s[1..].ToLowerInvariant();
    }

    // ************************************************************************************************
    private static string FormatAmount(string? amount)
    {
        var s = (amount ?? "").Trim();
        if (s.Length == 0 || string.Equals(s, "OPTIONAL AMOUNT", StringComparison.OrdinalIgnoreCase))
            return "";
        return s;
    }

    // ************************************************************************************************
    private static bool RowVisibleToUser(IEnumerable<EventItemAssignment> assignments, string userId) =>
        assignments.Any(a =>
            a.Status != AssignmentStatus.Removed &&
            ((a.UserId == userId &&
              (a.Status == AssignmentStatus.Assigned ||
               a.Status == AssignmentStatus.SignedUp ||
               a.Status == AssignmentStatus.Completed)) ||
             a.AssigneeType == AssigneeType.Everyone));

    // ************************************************************************************************
    private static bool RowVisibleToUser(IEnumerable<EventTaskAssignment> assignments, string userId) =>
        assignments.Any(a =>
            a.Status != AssignmentStatus.Removed &&
            ((a.UserId == userId &&
              (a.Status == AssignmentStatus.Assigned ||
               a.Status == AssignmentStatus.SignedUp ||
               a.Status == AssignmentStatus.Completed)) ||
             a.AssigneeType == AssigneeType.Everyone));

    // ************************************************************************************************
    private static bool IsDoneOnServer(IEnumerable<EventItemAssignment> assignments, string userId) =>
        assignments.Any(a => a.UserId == userId && a.Status == AssignmentStatus.Completed);

    // ************************************************************************************************
    private static bool IsDoneOnServer(IEnumerable<EventTaskAssignment> assignments, string userId) =>
        assignments.Any(a => a.UserId == userId && a.Status == AssignmentStatus.Completed);
}
