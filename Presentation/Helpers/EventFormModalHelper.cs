using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// Builds <see cref="EventDetailsViewModel"/> for items/tasks modals on the edit-event form.
/// <summary>Builds <see cref="EventDetailsViewModel"/> for items/tasks modals on the edit-event form.</summary>
public static class EventFormModalHelper
{
    // ************************************************************************************************
    // Creates a shell <see cref="EventDetailsViewModel"/> for the items/tasks modals on the edit-event form.
    public static EventDetailsViewModel CreateShell(Event eventData, string userId)
    {
        var model = eventData.MapTo<EventDetailsViewModel>();
        model.CanManageItemsTasks = eventData.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));
        model.CanAddItems = model.CanManageItemsTasks || model.AllowGuestBringItems;
        model.CanAddTasks = model.CanManageItemsTasks || model.AllowGuestTasks;
        model.CanClaimItems = model.CanManageItemsTasks;
        model.CanClaimTasks = model.CanManageItemsTasks;
        return model;
    }

    // ************************************************************************************************
    // Applies the items to the <see cref="EventDetailsViewModel"/>.
    public static void ApplyItems( EventDetailsViewModel model, IEnumerable<EventItem> items, Func<User?, string?> resolveDisplayName)
    {
        model.Items = items.Select(item =>
        {
            var row = item.MapTo<EventItemViewModel>();
            row.CreatedByDisplayName = item.CreatedByUser?.DisplayName;
            row.Assignments = item.Assignments.Select(a => new EventAssignmentSlotViewModel
            {
                Id = a.Id,
                AssigneeType = a.AssigneeType,
                UserId = a.UserId,
                DisplayName = resolveDisplayName(a.User),
                PlaceholderLabel = a.PlaceholderLabel,
                Status = a.Status,
            }).ToList();
            return row;
        }).ToList();
    }

    // ************************************************************************************************
    // Applies the tasks to the <see cref="EventDetailsViewModel"/>.
    public static void ApplyTasks( EventDetailsViewModel model, IEnumerable<EventTask> tasks, Func<User?, string?> resolveDisplayName)
    {
        model.Tasks = tasks.Select(task =>
        {
            var row = task.MapTo<EventTaskViewModel>();
            row.TaskLocation = task.TaskLocationName ?? task.TaskLocation?.Street;
            row.CreatedByDisplayName = task.CreatedByUser?.DisplayName;
            row.Assignments = task.Assignments.Select(a => new EventAssignmentSlotViewModel
            {
                Id = a.Id,
                AssigneeType = a.AssigneeType,
                UserId = a.UserId,
                DisplayName = resolveDisplayName(a.User),
                PlaceholderLabel = a.PlaceholderLabel,
                Status = a.Status,
            }).ToList();
            return row;
        }).ToList();
    }
}
