using Domain.Enums;
using Presentation.Extensions;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// EventOrganizerContributionsHelper — server-side builder for the owner/co-owner “Guests & contributions”
// table on event details. Mirrors FrontOffice `fillEventOrganizerView` in `eventdetailpage.js` so hosts see
// who brings what and who has which tasks without client-side demo JSON.
// ************************************************************************************************
// Consumers: `EventsController.EventDetails` (after items/tasks are loaded), `_EventDetailsGuestContributions.cshtml`.
// Data in: `EventDetailsViewModel.Items` / `.Tasks` assignments, `.Attendees`, `CanManageItemsTasks`.
// Data out: `OrganizerContributions`, `OrganizerContributionsTotalGuests`, `OrganizerContributionsShowFootnote`.
// ************************************************************************************************
public static class EventOrganizerContributionsHelper
{
    // ================================================================================================
    // Internal aggregation — groups assignment slots by person before rows are materialized
    // ================================================================================================

    // Mutable working set keyed by user or open-slot placeholder; not sent to the view.
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
    // ApplyTo — entry point; fills organizer contribution properties on the event details view model.
    // Builds table rows (person + item/task lines + checkmarks) for owners and co-owners because the
    // Razor partial expects pre-shaped data and assignment rows must be grouped per guest. Called from
    // `EventsController.EventDetails` only when `CanManageItemsTasks` is true, after `GetItemsForEventAsync`
    // and `GetTasksForEventAsync` so assignments are present. `EventDetails.cshtml` shows
    // `_EventDetailsGuestContributions` when `OrganizerContributions` is non-empty. Changing row order or
    // “Myself” labelling affects host UX only (not guest join, modals, or `HomeTodosHelper`).
    // `OrganizerContributionsShowFootnote` is set but the partial currently always shows the help text
    // under the table.
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
            var title = item.Title.FormatContributionTitle();
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
            var title = task.Title.FormatContributionTitle();
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
        var selfKey = currentUserId.UserKey();

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
            claimedNames.Add(bucket.DisplayName.NormalizePersonName());
            rows.Add(ToRow(bucket));
        }

        foreach (var attendee in model.Attendees
                     .Where(a => a.Status == AttendanceStatus.Accepted && !string.Equals(a.UserId, currentUserId, StringComparison.Ordinal))
                     .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var baseName = (attendee.DisplayName ?? "Guest").NormalizePersonName();
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
    // GetOrCreateBucket — resolves or creates the per-person accumulator for one assignment slot.
    // Maps an `EventAssignmentSlotViewModel` to a stable dictionary key and `PersonBucket` because one
    // guest may have several item/task assignments and buckets merge lines before a single table row.
    // Uses `OrganizerBucketKey`, `ResolveDisplayName`, and `ResolvePlusGuests`; called from `ApplyTo` item/task loops.
    // Wrong keys would duplicate rows or merge unrelated people in `_EventDetailsGuestContributions`.
    // ************************************************************************************************
    private static PersonBucket GetOrCreateBucket( Dictionary<string, PersonBucket> buckets, EventAssignmentSlotViewModel assignment, string currentUserId, EventDetailsViewModel model)
    {
        var key = assignment.OrganizerBucketKey();
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
    // ResolveDisplayName — label shown in the Person column for a bucket.
    // Returns placeholder text, “Myself” for the current user, assignment display name, or “Guest”, aligned
    // with FrontOffice (`appendAttendeeNameLabel` maps “You” → “Myself”) and item/task modals that use
    // `EventFormModalHelper.ResolveAssignmentDisplayName` for tags. Falls back to `model.Attendees` for names;
    // open slots use `PlaceholderLabel` (e.g. PERSON 1). Changing “Myself” vs display name logic affects only
    // this table, not chat or guest modal lists.
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
    // ResolvePlusGuests — extra headcount suffix (+N) beside a person name.
    // Returns `GuestCount - 1` from accepted attendance for that user, or 0 if not found, matching the join
    // flow, guests modal (`+N` in `_EventDetailsGuestsModal`), and FrontOffice roster lines. Uses
    // `EventDetailsViewModel.UserGuestCount` for the host’s own row and `model.Attendees` for others. Only
    // affects `PlusGuests` rendering in the contributions table and total guest math in `ApplyTo`.
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
    // MergeEveryoneLines — appends shared “EVERYONE” item/task titles to each person bucket.
    // Adds lines with `IsEveryoneLine = true` when not already listed for that person, matching FrontOffice
    // which shows everyone-tagged rows on each guest row unless they already have that title. Titles come from
    // `CollectEveryoneTitles`; styling lives in `_EventDetailsGuestContributions` and `eventitem.css`. If
    // everyone assignments are added or removed in modals, re-running `ApplyTo` on page load reflects changes.
    // ************************************************************************************************
    private static void MergeEveryoneLines( PersonBucket bucket, IReadOnlyList<string> everyoneItems, IReadOnlyList<string> everyoneTasks)
    {
        foreach (var title in everyoneItems)
        {
            if (bucket.Items.Any(l => l.Text.TitlesMatch(title)))
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
            if (bucket.Tasks.Any(l => l.Text.TitlesMatch(title)))
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
    // CollectEveryoneTitles — deduplicated uppercase titles for items/tasks assigned to Everyone.
    // Scans assignment lists for `AssigneeType.Everyone` (non-removed) and collects formatted titles because
    // everyone rows are excluded from per-bucket assignment loops and merged in a second pass. Same
    // “EVERYONE” concept as `HomeTodosHelper.RowVisibleToUser` and item/task modal tags. New everyone slots
    // in `EventItemService` and `EventTaskService` appear here after page refresh.
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

            var title = row.Title.FormatContributionTitle();
            if (string.IsNullOrWhiteSpace(title) || !seen.Add(title))
                continue;

            titles.Add(title);
        }

        return titles;
    }

    // ************************************************************************************************
    // AddContributionLine — adds or updates one cell line (item or task) inside a bucket.
    // Inserts `EventContributionsViewModel` line entries, merges duplicate titles, and ORs `IsDone` because
    // one person can claim the same item twice in edge cases and one checkmark is enough for the host view.
    // `IsDone` drives green check icons in the partial (same meaning as `HomeTodosHelper.IsDoneOnServer`).
    // `AssignmentStatus.Completed` from item/task signup flows controls checkmarks here on next GET.
    // ************************************************************************************************
    private static void AddContributionLine(
        List<EventContributionsViewModel> lines,
        string title,
        bool isEveryoneLine,
        bool isDone)
    {
        var existing = lines.FirstOrDefault(l => l.Text.TitlesMatch(title));
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
    // ToRow — maps an internal bucket to one `EventContributionsViewModel` table row.
    // Copies person fields and shallow-copies item/task line lists for Razor iteration. The view model uses
    // the same type for rows and nested lines; buckets keep building logic separate. `ApplyTo` orders rows
    // with Myself first, then assignees, then accepted guests without assignments. `IsPlaceholderPerson`
    // renders “—” in the Person column (`_EventDetailsGuestContributions`).
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
    // MapEveryoneOnlyLines — builds item/task lines when a row has no direct assignments (everyone-only).
    // Creates line entries for each everyone title with `IsEveryoneLine = true` so accepted guests with no
    // slots still see shared everyone duties on their row (FrontOffice parity). Used for empty “Myself” row
    // and attendee-only rows in `ApplyTo`. Does not set `IsDone`; everyone lines are never marked done here.
    // ************************************************************************************************
    private static List<EventContributionsViewModel> MapEveryoneOnlyLines(IEnumerable<string> titles) =>
        titles.Select(t => new EventContributionsViewModel
        {
            Text = t,
            IsEveryoneLine = true,
            IsDone = false,
        }).ToList();
}
