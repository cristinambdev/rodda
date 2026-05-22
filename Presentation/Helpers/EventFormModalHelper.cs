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

    // ************************************************************************************************
    // Merges event roles and attendances into one host-guest roster for the guests modal.
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
    // Resolves the display name for an assignment.
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
