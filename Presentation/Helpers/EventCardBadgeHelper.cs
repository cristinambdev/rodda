using Domain.Enums;
using Domain.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// EventCardBadgeHelper — claimed/unclaimed items/tasks pills and “you have items/tasks” pills on horizontal/vertical event cards.
// ************************************************************************************************
// Consumers: `EventsController.BuildCardBadgesAsync` → `ViewData["EventCardBadges"]` on All Events list;
//            `_HorizontalEventCard.cshtml`, `_VerticalEventCard.cshtml`.
// Distinct from: `AttendanceStatusLabels` (current-user RSVP badges on cards and event hero).
// ************************************************************************************************
public static class EventCardBadgeHelper
{
    // ************************************************************************************************
    // CardBadges — DTO for one event’s list-card workload pill state.
    // `WorkloadLabel` (unfilled count or “all filled”) and `UserHasItemsOrTasks` for “you” styling because record keeps `EventsController` dictionary typing explicit.
    // Uses `Compute` factory. Razor reads via `ViewData["EventCardBadges"]` keyed by event id.
    // ************************************************************************************************
    public sealed record CardBadges(string? WorkloadLabel, bool UserHasItemsOrTasks);

    // ************************************************************************************************
    // Compute — workload label and whether the current user has active item/task assignments.
    // Counts open PERSON slots on available rows; detects user assigned/signed-up slots because list cards surface planning status without opening event details (FrontOffice card parity).
    // Uses `EventItemService` / `EventTaskService` maintain assignments; skipped when `itemsTasksEnabled` false. Label-only on cards; does not block signup or change `EventOrganizerContributionsHelper`.
    // ************************************************************************************************
    public static CardBadges Compute(bool itemsTasksEnabled, string userId, IEnumerable<EventItem> items, IEnumerable<EventTask> tasks)
    {
        if (!itemsTasksEnabled)
            return new CardBadges(null, false);

        var itemList = items.Where(i => i.IsActive).ToList();
        var taskList = tasks.Where(t => t.IsActive).ToList();
        var unfilled = CountUnfilledSlots(itemList) + CountUnfilledSlots(taskList);

        string? workload = unfilled > 0
            ? $"{unfilled} unfilled items/tasks"
            : itemList.Count > 0 || taskList.Count > 0
                ? "All items/tasks are filled"
                : null;

        var userHasItemsOrTasks = itemList.Any(i => UserHasActiveAssignment(i.Assignments, userId))
            || taskList.Any(t => UserHasActiveAssignment(t.Assignments, userId));

        return new CardBadges(workload, userHasItemsOrTasks);
    }

    // ************************************************************************************************
    // CountUnfilledSlots (items) — sum of open signup slots across active items.
    // Delegates per-item to `CountOpenSlots` for item assignments because workload pill aggregates planning gaps for hosts/members scanning the list.
    // Uses `Compute`. Card badge text only.
    // ************************************************************************************************
    private static int CountUnfilledSlots(IEnumerable<EventItem> items)
    {
        if (!items.Any())
            return 0;

        return items.Sum(i => CountOpenSlots(i.SignupMode, i.Assignments));
    }

    // ************************************************************************************************
    // CountUnfilledSlots (tasks) — sum of open signup slots across active tasks.
    // Parallel to item overload for tasks because tasks contribute to the same workload metric.
    // Uses `Compute`. Card badge text only.
    // ************************************************************************************************
    private static int CountUnfilledSlots(IEnumerable<EventTask> tasks)
    {
        if (!tasks.Any())
            return 0;

        return tasks.Sum(t => CountOpenSlots(t.SignupMode, t.Assignments));
    }

    // ************************************************************************************************
    // CountOpenSlots (items) — open PERSON slots still available for signup.
    // Counts non-removed open-slot assignments with no `UserId` when signup mode is Available because matches modal “available” rows guests can claim.
    // Uses `EventItemService` creates open slots from `PeopleNeeded`. Unfilled count on cards; claiming updates count on next list load.
    // ************************************************************************************************
    private static int CountOpenSlots(SignupMode signupMode, IEnumerable<EventItemAssignment> assignments)
    {
        if (signupMode != SignupMode.Available)
            return 0;

        return assignments.Count(a =>
            a.Status != AssignmentStatus.Removed
            && a.AssigneeType == AssigneeType.OpenSlot
            && string.IsNullOrEmpty(a.UserId));
    }

    // ************************************************************************************************
    // CountOpenSlots (tasks) — open slots for tasks.
    // Parallel to item overload because same signup semantics as bring-items.
    // Uses `EventTaskService`. Card badge text only.
    // ************************************************************************************************
    private static int CountOpenSlots(SignupMode signupMode, IEnumerable<EventTaskAssignment> assignments)
    {
        if (signupMode != SignupMode.Available)
            return 0;

        return assignments.Count(a =>
            a.Status != AssignmentStatus.Removed
            && a.AssigneeType == AssigneeType.OpenSlot
            && string.IsNullOrEmpty(a.UserId));
    }

    // ************************************************************************************************
    // UserHasActiveAssignment (items) — user is on an item row (assigned or signed up, not completed-only).
    // True if any non-removed assignment for `userId` is Assigned or SignedUp because drives “you” highlight on cards (`UserHasItemsOrTasks`); completed-only may not need highlight.
    // Uses `HomeTodosHelper` also shows completed rows; card highlight is for active commitment. CSS/class on card partials only.
    // ************************************************************************************************
    private static bool UserHasActiveAssignment(IEnumerable<EventItemAssignment> assignments, string userId)
    {
        if (!assignments.Any())
            return false;

        return assignments.Any(a =>
            a.UserId == userId
            && a.Status is AssignmentStatus.Assigned or AssignmentStatus.SignedUp);
    }

    // ************************************************************************************************
    // UserHasActiveAssignment (tasks) — user is on a task row.
    // Parallel to item overload because either items or tasks sets `UserHasItemsOrTasks`.
    // Uses `Compute`. Card styling only.
    // ************************************************************************************************
    private static bool UserHasActiveAssignment(IEnumerable<EventTaskAssignment> assignments, string userId)
    {
        if (!assignments.Any())
            return false;

        return assignments.Any(a =>
            a.UserId == userId
            && a.Status is AssignmentStatus.Assigned or AssignmentStatus.SignedUp);
    }
}
