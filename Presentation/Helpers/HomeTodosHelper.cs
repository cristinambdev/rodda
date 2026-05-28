using System.Globalization;
using Domain.Enums;
using Domain.Models;
using Presentation.Extensions;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// HomeTodosHelper — builds the server-rendered Home “My To-Dos” page from item/task assignments.
// Mirrors FrontOffice home todo cards (`index.js` / home HTML): what the signed-in user should bring or do.
// ************************************************************************************************
// Consumers: `HomeController.Index`, `Views/Home/Index.cshtml`.
// Data in: `EventItem` / `EventTask` rows (with assignments) from services; `userId` from auth.
// Data out: `HomeIndexViewModel` (filtered cards, range, empty message).
// ************************************************************************************************
public static class HomeTodosHelper
{
    // ************************************************************************************************
    // Builds a home todos page view model for the active date range.
    // Groups visible items/tasks into per-event cards, filters by week/month/year, alternates card themes.
    // As Home is read-only summary; guests need one place to see obligations before opening event details.
    // Uses `HomeController` to load items/tasks; card links use `EventUrlExtensions.DetailsPath`.
    // Changing visibility rules affects who sees cards, not event details or organizer table.
    // `IsDoneOnServer` ties checkmarks to `AssignmentStatus.Completed` (same DB flag as contributions table).
    // ************************************************************************************************
    public static HomeIndexViewModel Build(IEnumerable<EventItem> items, IEnumerable<EventTask> tasks, string userId, string? rangeRaw)
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
    // BuildEventCards — one card per event that has at least one visible bring line or task line for the user.
    // Filters items/tasks, groups by `EventId`, formats date/location copy and done counts.
    // A user may have multiple rows on the same event; the UI shows a single card with a combined checklist.
    // Uses `RowVisibleToUser`, `FormatLineTitle`, `FormatAmount`, `IsDoneOnServer`, `EventUrlExtensions`.
    // Drives HTML structure on Home only; does not write assignments or attendance.
    // ************************************************************************************************
    private static List<HomeTodoEventCardViewModel> BuildEventCards(
        IEnumerable<EventItem> items,
        IEnumerable<EventTask> tasks,
        string userId)
    {
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
                ItemId = i.Id,
                Title = FormatLineTitle(i.Title),
                Amount = FormatAmount(i.Amount),
                Done = IsDoneOnServer(i.Assignments, userId),
            }).ToList();

            var taskLines = eventTasks.Select(t => new HomeTodoTaskLineViewModel
            {
                TaskId = t.Id,
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
                DetailUrl = eventId.DetailsPath(slug),
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
    // NormalizeRange — maps query string to `week` | `month` | `year` (default month).
    // Sanitizes `?range=` from the home page filter links.
    // Keeps invalid values from breaking `IsInRange` / empty-state copy.
    // Uses `Home/Index.cshtml` range tabs; `EmptyMessageForRange`, `IsInRange`.
    // UI-only; no persistence.
    // ************************************************************************************************
    private static string NormalizeRange(string? range) =>
        range switch
        {
            "week" => "week",
            "year" => "year",
            _ => "month",
        };

    // ************************************************************************************************
    // EmptyMessageForRange — copy when no todo cards match the selected range.
    // Returns a short empty-state string per range.
    // Gives context when filtering hides all cards (not the same as “no assignments globally”).
    // Uses `Build` to set `HomeIndexViewModel.EmptyMessage`.
    // Home page text only.
    // ************************************************************************************************
    private static string EmptyMessageForRange(string range) =>
        range switch
        {
            "week" => "Nothing on your plate this week.",
            "year" => "Nothing on your plate this year.",
            _ => "Nothing on your plate this month.",
        };

    // ************************************************************************************************
    // IsInRange — whether an event start date falls in the selected home filter window.
    // Excludes past-before-today; week = next 7 days, month/year = calendar boundaries (local time).
    // Matches FrontOffice home todo range behaviour for upcoming obligations.
    // Uses `Build` to filter cards after `BuildEventCards`.
    // Hiding cards here does not cancel assignments or change list pages.
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
    // FormatLineTitle — title case for todo line labels (first letter upper, rest lower).
    // Formats item/task titles on home cards.
    // Softer copy on home vs uppercase `FormatContributionTitle` on organizer contributions table.
    // Used only in `BuildEventCards` bring/task lines.
    // Display-only on Home.
    // ************************************************************************************************
    private static string FormatLineTitle(string title)
    {
        var s = (title ?? "").Trim();
        if (s.Length == 0) return "";
        return char.ToUpper(s[0]) + s[1..].ToLowerInvariant();
    }

    // ************************************************************************************************
    // FormatAmount — hides empty or “OPTIONAL AMOUNT” amount labels on bring lines.
    // Returns display amount or empty string.
    // Optional amounts should not clutter the home checklist.
    // Uses `EventItem` domain field.
    // Home card subtext only.
    // ************************************************************************************************
    private static string FormatAmount(string? amount)
    {
        var s = (amount ?? "").Trim();
        if (s.Length == 0 || string.Equals(s, "OPTIONAL AMOUNT", StringComparison.OrdinalIgnoreCase))
            return "";
        return s;
    }

    // ************************************************************************************************
    // RowVisibleToUser (items) — whether a bring-item row belongs on the user’s home todo list.
    // True if user has active assignment (assigned/signed up/completed) or row is tagged Everyone.
    // Same inclusion rule as FrontOffice “Your to-dos” / organizer self rows (`organizerTodoRowHasYouOrEveryone`).
    // Uses `EventOrganizerContributionsHelper` everyone lines; item modal signup in `EventItemService`.
    // Omitting a row here only hides it from Home, not from event modals or host table.
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
    // RowVisibleToUser (tasks) — same visibility rule for guest task rows.
    // Parallel to item overload for `EventTaskAssignment`.
    // Tasks and items share assignment semantics in the domain.
    // Uses `EventTaskService` claim/signup flows update statuses checked here.
    // Home task lines only.
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
    // IsDoneOnServer (items) — whether the user marked this bring row completed in the app.
    // True when any assignment for `userId` has `AssignmentStatus.Completed`.
    // Server truth for done checkmarks; FrontOffice demo used localStorage, BackOffice uses DB.
    // Uses `EventOrganizerContributionsHelper.AddContributionLine` host view checkmarks; item modal POST.
    // Updating completion in item flows refreshes Home and contributions table on next load.
    // ************************************************************************************************
    private static bool IsDoneOnServer(IEnumerable<EventItemAssignment> assignments, string userId) =>
        assignments.Any(a => a.UserId == userId && a.Status == AssignmentStatus.Completed);

    // ************************************************************************************************
    // IsDoneOnServer (tasks) — completion flag for task assignments.
    // Parallel to item overload.
    // Keeps task done state consistent with bring items.
    // Uses `EventTaskService` completion endpoints.
    // Home task strikethrough/check state; host contributions `IsDone` when viewing that user’s rows.
    // ************************************************************************************************
    private static bool IsDoneOnServer(IEnumerable<EventTaskAssignment> assignments, string userId) =>
        assignments.Any(a => a.UserId == userId && a.Status == AssignmentStatus.Completed);
}
