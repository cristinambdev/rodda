using Domain.Enums;
using Domain.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// Workload / “you” pills on horizontal event list cards.
public static class EventCardBadgeHelper
{
    // ************************************************************************************************
    // Record representing the workload / “you” pills on horizontal event list cards.
    public sealed record CardBadges(string? WorkloadLabel, bool UserHasItemsOrTasks);

    // ************************************************************************************************
    // Computes the workload / “you” pills on horizontal event list cards.
    public static CardBadges Compute( bool itemsTasksEnabled, string userId, IEnumerable<EventItem> items, IEnumerable<EventTask> tasks)
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
    // Counts the unfilled slots for a list of items.
    private static int CountUnfilledSlots(IEnumerable<EventItem> items)
    {
        if (!items.Any())
            return 0;

        return items.Sum(i => CountOpenSlots(i.SignupMode, i.Assignments));
    }

    // ************************************************************************************************
    // Counts the unfilled slots for a list of tasks.
    private static int CountUnfilledSlots(IEnumerable<EventTask> tasks)
    {
        if (!tasks.Any())
            return 0;

        return tasks.Sum(t => CountOpenSlots(t.SignupMode, t.Assignments));
    }
    // ************************************************************************************************
    // Counts the open slots for a list of items.
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
    // Counts the open slots for a list of tasks.
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
    // Checks if the user has an active assignment for a list of items.
    private static bool UserHasActiveAssignment(IEnumerable<EventItemAssignment> assignments, string userId)
    {
        if (!assignments.Any())
            return false;

        return assignments.Any(a =>
            a.UserId == userId
            && a.Status is AssignmentStatus.Assigned or AssignmentStatus.SignedUp);
    }

    // ************************************************************************************************
    // Checks if the user has an active assignment for a list of tasks.
    private static bool UserHasActiveAssignment(IEnumerable<EventTaskAssignment> assignments, string userId)
    {
        if (!assignments.Any())
            return false;

        return assignments.Any(a =>
            a.UserId == userId
            && a.Status is AssignmentStatus.Assigned or AssignmentStatus.SignedUp);
    }
}
