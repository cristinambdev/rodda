using Domain.Enums;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// Builds the owner/co-owner "Guests & contributions" table on event details (FrontOffice parity).
public static class EventOrganizerContributionsHelper
{
    private sealed class PersonBucket
    {
        public string Key { get; init; } = "";
        public string DisplayName { get; set; } = "";
        public bool IsSelf { get; set; }
        public bool IsPlaceholder { get; set; }
        public int PlusGuests { get; set; }
        public List<EventContributionsViewModel> Items { get; } = new();
        public List<EventContributionsViewModel> Tasks { get; } = new();
    }

    // ************************************************************************************************
    public static void ApplyTo(EventDetailsViewModel model, string currentUserId)
    {
        model.OrganizerContributions = new List<EventContributionsViewModel>();
        model.OrganizerContributionsTotalGuests = 0;
        model.OrganizerContributionsShowFootnote = false;

        if (!model.CanManageItemsTasks || string.IsNullOrWhiteSpace(currentUserId))
            return;

        var buckets = new Dictionary<string, PersonBucket>(StringComparer.OrdinalIgnoreCase);
        var everyoneItemTitles = CollectEveryoneTitles(model.Items.Select(i => (i.Title, i.Assignments)));
        var everyoneTaskTitles = CollectEveryoneTitles(model.Tasks.Select(t => (t.Title, t.Assignments)));

        foreach (var item in model.Items)
        {
            var title = FormatContributionTitle(item.Title);
            if (string.IsNullOrWhiteSpace(title))
                continue;

            foreach (var assignment in item.Assignments.Where(a => a.Status != AssignmentStatus.Removed))
            {
                if (assignment.AssigneeType == AssigneeType.Everyone)
                    continue;

                var bucket = GetOrCreateBucket(buckets, assignment, currentUserId, model);
                AddContributionLine(
                    bucket.Items,
                    title,
                    isEveryoneLine: false,
                    isDone: assignment.Status == AssignmentStatus.Completed);
            }
        }

        foreach (var task in model.Tasks)
        {
            var title = FormatContributionTitle(task.Title);
            if (string.IsNullOrWhiteSpace(title))
                continue;

            foreach (var assignment in task.Assignments.Where(a => a.Status != AssignmentStatus.Removed))
            {
                if (assignment.AssigneeType == AssigneeType.Everyone)
                    continue;

                var bucket = GetOrCreateBucket(buckets, assignment, currentUserId, model);
                AddContributionLine(
                    bucket.Tasks,
                    title,
                    isEveryoneLine: false,
                    isDone: assignment.Status == AssignmentStatus.Completed);
            }
        }

        foreach (var bucket in buckets.Values)
            MergeEveryoneLines(bucket, everyoneItemTitles, everyoneTaskTitles);

        var rows = new List<EventContributionsViewModel>();
        var selfKey = UserKey(currentUserId);

        if (buckets.TryGetValue(selfKey, out var selfBucket))
        {
            selfBucket.IsSelf = true;
            selfBucket.DisplayName = "Myself";
            selfBucket.PlusGuests = ResolvePlusGuests(currentUserId, model);
            rows.Add(ToRow(selfBucket));
        }
        else
        {
            rows.Add(new EventContributionsViewModel
            {
                PersonName = "Myself",
                PlusGuests = ResolvePlusGuests(currentUserId, model),
                IsSelf = true,
                Items = MapEveryoneOnlyLines(everyoneItemTitles),
                Tasks = MapEveryoneOnlyLines(everyoneTaskTitles),
            });
        }

        var claimedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Myself",
        };

        foreach (var bucket in buckets.Values
                     .Where(b => !b.IsSelf)
                     .OrderBy(b => b.IsPlaceholder ? 1 : 0)
                     .ThenBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            claimedNames.Add(NormalizePersonName(bucket.DisplayName));
            rows.Add(ToRow(bucket));
        }

        foreach (var attendee in model.Attendees
                     .Where(a => a.Status == AttendanceStatus.Accepted && !string.Equals(a.UserId, currentUserId, StringComparison.Ordinal))
                     .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var baseName = NormalizePersonName(attendee.DisplayName ?? "Guest");
            if (claimedNames.Contains(baseName))
                continue;

            claimedNames.Add(baseName);
            var extra = Math.Max(0, attendee.GuestCount - 1);
            rows.Add(new EventContributionsViewModel
            {
                PersonName = string.IsNullOrWhiteSpace(attendee.DisplayName) ? "Guest" : attendee.DisplayName.Trim(),
                PlusGuests = extra,
                Items = MapEveryoneOnlyLines(everyoneItemTitles),
                Tasks = MapEveryoneOnlyLines(everyoneTaskTitles),
            });
        }

        var totalGuests = model.Attendees
            .Where(a => a.Status == AttendanceStatus.Accepted)
            .Sum(a => Math.Max(1, a.GuestCount));

        if (totalGuests <= 0)
            totalGuests = Math.Max(rows.Count, 1);

        model.OrganizerContributions = rows;
        model.OrganizerContributionsTotalGuests = totalGuests;
        model.OrganizerContributionsShowFootnote = rows.Any(r =>
            r.Items.Any(l => l.IsDone) || r.Tasks.Any(l => l.IsDone));
    }

    // ************************************************************************************************
    private static PersonBucket GetOrCreateBucket(
        Dictionary<string, PersonBucket> buckets,
        EventAssignmentSlotViewModel assignment,
        string currentUserId,
        EventDetailsViewModel model)
    {
        var key = BucketKey(assignment);
        if (!buckets.TryGetValue(key, out var bucket))
        {
            bucket = new PersonBucket
            {
                Key = key,
                IsSelf = assignment.AssigneeType == AssigneeType.User
                    && string.Equals(assignment.UserId, currentUserId, StringComparison.Ordinal),
                IsPlaceholder = assignment.AssigneeType == AssigneeType.OpenSlot,
                DisplayName = ResolveDisplayName(assignment, currentUserId, model),
                PlusGuests = assignment.AssigneeType == AssigneeType.User && !string.IsNullOrWhiteSpace(assignment.UserId)
                    ? ResolvePlusGuests(assignment.UserId!, model)
                    : 0,
            };
            buckets[key] = bucket;
        }

        return bucket;
    }

    // ************************************************************************************************
    private static string ResolveDisplayName(
        EventAssignmentSlotViewModel assignment,
        string currentUserId,
        EventDetailsViewModel model)
    {
        if (assignment.AssigneeType == AssigneeType.OpenSlot)
            return (assignment.PlaceholderLabel ?? "Open slot").Trim();

        if (string.Equals(assignment.UserId, currentUserId, StringComparison.Ordinal))
            return "Myself";

        if (!string.IsNullOrWhiteSpace(assignment.DisplayName))
            return assignment.DisplayName.Trim();

        var fromAttendance = model.Attendees.FirstOrDefault(a =>
            string.Equals(a.UserId, assignment.UserId, StringComparison.Ordinal));
        if (!string.IsNullOrWhiteSpace(fromAttendance?.DisplayName))
            return fromAttendance.DisplayName.Trim();

        return "Guest";
    }

    // ************************************************************************************************
    private static int ResolvePlusGuests(string userId, EventDetailsViewModel model)
    {
        var attendance = model.Attendees.FirstOrDefault(a =>
            string.Equals(a.UserId, userId, StringComparison.Ordinal));
        if (attendance == null)
            return 0;

        return Math.Max(0, attendance.GuestCount - 1);
    }

    // ************************************************************************************************
    private static void MergeEveryoneLines(
        PersonBucket bucket,
        IReadOnlyList<string> everyoneItems,
        IReadOnlyList<string> everyoneTasks)
    {
        foreach (var title in everyoneItems)
        {
            if (bucket.Items.Any(l => TitlesMatch(l.Text, title)))
                continue;

            bucket.Items.Add(new EventContributionsViewModel
            {
                Text = title,
                IsEveryoneLine = true,
                IsDone = false,
            });
        }

        foreach (var title in everyoneTasks)
        {
            if (bucket.Tasks.Any(l => TitlesMatch(l.Text, title)))
                continue;

            bucket.Tasks.Add(new EventContributionsViewModel
            {
                Text = title,
                IsEveryoneLine = true,
                IsDone = false,
            });
        }
    }

    // ************************************************************************************************
    private static List<string> CollectEveryoneTitles(
        IEnumerable<(string Title, List<EventAssignmentSlotViewModel> Assignments)> rows)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var titles = new List<string>();

        foreach (var row in rows)
        {
            if (!row.Assignments.Any(a =>
                    a.AssigneeType == AssigneeType.Everyone && a.Status != AssignmentStatus.Removed))
                continue;

            var title = FormatContributionTitle(row.Title);
            if (string.IsNullOrWhiteSpace(title) || !seen.Add(title))
                continue;

            titles.Add(title);
        }

        return titles;
    }

    // ************************************************************************************************
    private static void AddContributionLine(
        List<EventContributionsViewModel> lines,
        string title,
        bool isEveryoneLine,
        bool isDone)
    {
        var existing = lines.FirstOrDefault(l => TitlesMatch(l.Text, title));
        if (existing != null)
        {
            existing.IsDone = existing.IsDone || isDone;
            return;
        }

        lines.Add(new EventContributionsViewModel
        {
            Text = title,
            IsEveryoneLine = isEveryoneLine,
            IsDone = isDone,
        });
    }

    // ************************************************************************************************
    private static EventContributionsViewModel ToRow(PersonBucket bucket) =>
        new()
        {
            PersonName = bucket.DisplayName,
            PlusGuests = bucket.PlusGuests,
            IsSelf = bucket.IsSelf,
            IsPlaceholderPerson = bucket.IsPlaceholder,
            Items = bucket.Items.ToList(),
            Tasks = bucket.Tasks.ToList(),
        };

    // ************************************************************************************************
    private static List<EventContributionsViewModel> MapEveryoneOnlyLines(IEnumerable<string> titles) =>
        titles.Select(t => new EventContributionsViewModel
        {
            Text = t,
            IsEveryoneLine = true,
            IsDone = false,
        }).ToList();

    // ************************************************************************************************
    private static string FormatContributionTitle(string? title)
    {
        var s = (title ?? "").Trim();
        return s.Length == 0 ? "" : s.ToUpperInvariant();
    }

    // ************************************************************************************************
    private static string BucketKey(EventAssignmentSlotViewModel assignment) =>
        assignment.AssigneeType switch
        {
            AssigneeType.OpenSlot => SlotKey(assignment.PlaceholderLabel),
            _ => UserKey(assignment.UserId ?? assignment.Id),
        };

    // ************************************************************************************************
    private static string UserKey(string userId) => $"user:{userId}";

    // ************************************************************************************************
    private static string SlotKey(string? placeholder) =>
        $"slot:{(placeholder ?? "").Trim().ToUpperInvariant()}";

    // ************************************************************************************************
    private static string NormalizePersonName(string name) =>
        name.Replace("(host)", "", StringComparison.OrdinalIgnoreCase).Trim();

    // ************************************************************************************************
    private static bool TitlesMatch(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
