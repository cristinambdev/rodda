using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Presentation.Models;

namespace Presentation.Helpers;

// ************************************************************************************************
// EventFormModalHelper — builds `EventDetailsViewModel` shells for items/tasks modals and host guest lists.
// ************************************************************************************************
// Consumers: `EventsController` (event details, edit form modals), item/task partials, guest modal.
// Shared with: `EventOrganizerContributionsHelper.ResolveDisplayName` pattern via `ResolveAssignmentDisplayName`.
// Permission flags here gate `_EventItemsModal`, `_EventTasksModal`, and claim/add buttons in UI.
// ************************************************************************************************
public static class EventFormModalHelper
{
    // ************************************************************************************************
    // CreateShell — minimal event details model with manage/claim permissions for modals.
    // Maps event via AutoMapper; sets `CanManageItemsTasks`, `CanAdd*` / `CanClaim*` from roles and flags because edit-event page embeds items/tasks modals before full item/task lists are loaded.
    // Uses `EventsController.BuildEventDetailsModalAsync`; same role rules as `EventDetails` action. False permissions hide add/claim UI in modals without affecting read-only guests on details page.
    // ************************************************************************************************
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
    // ApplyItems — maps domain items (with assignments) onto the modal view model.
    // Produces `EventItemViewModel` rows with `EventAssignmentSlotViewModel` tags for each slot because Razor item rows/modals need display names and slot state separate from domain entities.
    // Uses `EventsController.EventDetails` and edit modal; `EventItemService` source data. Drives `_EventItemRow` / modal list; assignment changes refresh on redirect or reload.
    // ************************************************************************************************
    public static void ApplyItems(EventDetailsViewModel model, IEnumerable<EventItem> items, Func<User?, string?> resolveDisplayName)
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
    // ApplyTasks — maps domain tasks (with assignments) onto the modal view model.
    // Same as `ApplyItems` for `EventTask` / task location display because tasks modals share the same assignment slot shape as items.
    // Uses `EventTaskService`; `EventOrganizerContributionsHelper` reads same assignment data on details. Task modal UI; host contributions table task column after page load.
    // ************************************************************************************************
    public static void ApplyTasks(EventDetailsViewModel model, IEnumerable<EventTask> tasks, Func<User?, string?> resolveDisplayName)
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

    // ************************************************************************************************
    // BuildHostGuestRoster — merged roles + attendances for the host guests modal.
    // One row per user: display name, role label (Owner/Co-owner), attendance status, guest count because hosts
    // need RSVP overview; list order puts creator and owners first.
    // Uses `_EventDetailsGuestsModal.cshtml`; `EventDetails` sets `HostGuestRoster` when `CanManageItemsTasks`.
    // Changing sort or merge logic affects modal only; join POST updates underlying attendance rows.
    // ************************************************************************************************
    public static List<EventGuestRosterViewModel> BuildHostGuestRoster(Event eventData)
    {
        var byUser = new Dictionary<string, EventGuestRosterViewModel>(StringComparer.Ordinal);

        foreach (var role in eventData.Roles)
        {
            var roleLabel = role.Role == EventRoleType.Owner ? "Owner" : "Co-owner";
            byUser[role.UserId] = new EventGuestRosterViewModel
            {
                UserId = role.UserId,
                DisplayName = ResolveAssignmentDisplayName(role.User),
                RoleLabel = roleLabel,
                Status = AttendanceStatus.Pending,
                GuestCount = 1,
            };
        }

        foreach (var attendance in eventData.Attendances)
        {
            if (byUser.TryGetValue(attendance.UserId, out var row))
            {
                row.Status = attendance.Status;
                row.GuestCount = Math.Max(1, attendance.GuestCount);
                if (string.IsNullOrWhiteSpace(row.DisplayName))
                    row.DisplayName = ResolveAssignmentDisplayName(attendance.User);
            }
            else
            {
                byUser[attendance.UserId] = new EventGuestRosterViewModel
                {
                    UserId = attendance.UserId,
                    DisplayName = ResolveAssignmentDisplayName(attendance.User),
                    GuestCount = Math.Max(1, attendance.GuestCount),
                    Status = attendance.Status,
                };
            }
        }

        return byUser.Values
            .OrderByDescending(r => string.Equals(r.UserId, eventData.CreatedByUserId, StringComparison.Ordinal))
            .ThenByDescending(r => r.RoleLabel == "Owner")
            .ThenByDescending(r => r.RoleLabel == "Co-owner")
            .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ************************************************************************************************
    // ResolveAssignmentDisplayName — display name for assignment tags and roster rows.
    // Prefers `User.DisplayName`, then email; null if no user.
    // It is used as a delegate in `ApplyItems`/`ApplyTasks` and in `EventsController` mappings to build assignment
    // tags and roster rows.
    // Renaming users in DB updates all assignment labels on next request; open slots use placeholder instead.
    // ************************************************************************************************
    public static string? ResolveAssignmentDisplayName(User? user)
    {
        if (user == null)
            return null;

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return user.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email.Trim();

        return null;
    }
}
