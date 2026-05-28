using Business.Dtos;
using Data.Entities;
using Data.Repositories;
using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Business.Services;

public interface IEventItemService
{
    Task<EventItemResult> CreateEventItemAsync(string userId, AddItemFormData formData);
    Task<EventItemResult> DeleteEventItemAsync(string userId, string eventId, string eventItemId);
    Task<EventItemResult<EventItem>> GetEventItemAsync(string eventId, string eventItemId);
    Task<EventItemResult<IEnumerable<EventItem>>> GetItemsClaimedByUserAsync(string userId);
    Task<EventItemResult<IEnumerable<EventItem>>> GetItemsForHomeTodosAsync(string userId);
    Task<EventItemResult<IEnumerable<EventItem>>> GetItemsForEventAsync(string userId, string eventId);
    Task<EventItemResult> UpdateEventItemAsync(string userId, string eventId, string eventItemId, EditItemFormData formData);
    Task<EventItemResult> ClaimItemAsync(string userId, string eventId, string eventItemId, string? assignmentId = null);
    Task<EventItemResult> UnclaimItemAsync(string userId, string eventId, string eventItemId, string? assignmentId = null);
    Task<EventItemResult<bool>> ToggleItemCompletionAsync(string userId, string eventId, string eventItemId);
}

// Handles EventItemEntity, EventItemAssignmentEntity

public class EventItemService( IEventItemRepository eventItemRepository, IEventItemAssignmentRepository eventItemAssignmentRepository, IEventRepository eventRepository, IEventAccessService eventAccessService) : IEventItemService
{
    private readonly IEventItemRepository _eventItemRepository = eventItemRepository;
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IEventItemAssignmentRepository _eventItemAssignmentRepository = eventItemAssignmentRepository;
    private readonly IEventAccessService _eventAccessService = eventAccessService;

    // **************************************************************************************************************************
    // CREATE
    public async Task<EventItemResult> CreateEventItemAsync(string userId, AddItemFormData formData)
    {
        if (formData == null)
            return new EventItemResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        // Fetch the event with roles and attendances to check permissions
        var fetchResponse = await _eventRepository.GetEntityAsync
        (
            e => e.Id == formData.EventId,
            includes: [x => x.Roles, x => x.Attendances]
        );
        var fetchEvent = fetchResponse.Result;
        if (!fetchResponse.Succeeded || fetchEvent == null)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Event with id {formData.EventId} was not found" };

        if (!_eventAccessService.HasViewAccess(fetchEvent, userId))
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var permissionError = ValidateCreateItemPermission(fetchEvent, userId);
        if (permissionError != null)
            return permissionError;

        var newEventItem = formData.MapTo<EventItemEntity>();
        newEventItem.EventId = formData.EventId;
        newEventItem.CreatedByUserId = userId;
        newEventItem.IsActive = true;
        newEventItem.CreatedAt = DateTime.UtcNow;

        var peopleNeeded = ClampPeopleNeeded(formData.PeopleNeeded);
        newEventItem.PeopleNeeded = peopleNeeded;
        newEventItem.OriginalTitle = formData.Title;
        newEventItem.OriginalAmount = formData.Amount;
        newEventItem.OriginalPeopleNeeded = peopleNeeded;
        newEventItem.OriginalSignupMode = formData.SignupMode;

        var result = await _eventItemRepository.AddAsync(newEventItem);
        if (!result.Succeeded)
            return new EventItemResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };

        for (var slot = 1; slot <= peopleNeeded; slot++)
        {
            var slotResult = await _eventItemAssignmentRepository.AddAsync(CreateOpenSlotAssignment(newEventItem.Id, slot));
            if (!slotResult.Succeeded)
                return new EventItemResult { Succeeded = false, StatusCode = slotResult.StatusCode, ErrorMessage = slotResult.ErrorMessage };
        }

        return new EventItemResult { Succeeded = true, StatusCode = 201 };
    }

    // **************************************************************************************************************************
    // READ
    public async Task<EventItemResult<EventItem>> GetEventItemAsync(string eventId, string eventItemId)
    {
        // Query the repository for a matching item belonging to the specified event, including its current claims
        var response = await _eventItemRepository.GetAsync
            (
                where: e => e.Id == eventItemId && e.EventId == eventId,
                includes: [x => x.Assignments]
            );

        return response.Succeeded
            ? new EventItemResult<EventItem> { Succeeded = true, StatusCode = 200, Result = response.Result }
            : new EventItemResult<EventItem> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event item not found." };
    }

    // **************************************************************************************************************************
    // READ: Get all items for an event page
    public async Task<EventItemResult<IEnumerable<EventItem>>> GetItemsForEventAsync(string userId, string eventId)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventItemResult<IEnumerable<EventItem>> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Grab only active (non-soft-deleted) items for this event, complete with user assignments
        var response = await _eventItemRepository.GetAllAsync
            (
                selector: i => i,
                where: i => i.EventId == eventId && i.IsActive,
                sortBy: i => i.SortOrder,
                includeChains:
                [
                    q => q.Include(i => i.Assignments).ThenInclude(a => a.User),
                    q => q.Include(i => i.CreatedByUser)
                ]
            );

        if (!response.Succeeded || response.Result == null)
            return new EventItemResult<IEnumerable<EventItem>>
            { Succeeded = false, StatusCode = response.StatusCode, ErrorMessage = response.ErrorMessage ?? "Could not load event items." };

        var items = response.Result.Select(entity =>
        {
            var item = entity.MapTo<EventItem>();
            item.Assignments = entity.Assignments.Select(a =>
            {
                var assignment = a.MapTo<EventItemAssignment>();
                assignment.User = a.User?.MapTo<User>();
                return assignment;
            }).ToList();
            item.CreatedByUser = entity.CreatedByUser?.MapTo<User>();
            return item;
        });

        return new EventItemResult<IEnumerable<EventItem>>
        { Succeeded = true, StatusCode = 200, Result = items };
    }

    // **************************************************************************************************************************
    // READ: Get all items claimed by a specific user for their To-Do Dashboard
    public async Task<EventItemResult<IEnumerable<EventItem>>> GetItemsClaimedByUserAsync(string userId)
    {
        // Extract items where the user is actively locked into an 'Assigned' or 'SignedUp' tracking status
        var response = await _eventItemRepository.GetAllAsync
            (
                selector: i => i,
                where: i => i.IsActive && i.Assignments.Any(a =>
                    a.UserId == userId &&
                    (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.SignedUp)),
                sortBy: i => i.CreatedAt,
                includes: [x => x.Event, x => x.Assignments]
                );

        if (!response.Succeeded || response.Result == null)
            return new EventItemResult<IEnumerable<EventItem>>
            { Succeeded = false, StatusCode = response.StatusCode, ErrorMessage = response.ErrorMessage ?? "Could not load claimed items." };

        // Map the structural model and manually bind the unmapped parent event title
        var items = response.Result.Select(entity =>
        {
            var item = entity.MapTo<EventItem>();
            item.EventTitle = entity.Event?.Title;
            return item;
        });

        return new EventItemResult<IEnumerable<EventItem>> { Succeeded = true, StatusCode = 200, Result = items };
    }

    // **************************************************************************************************************************
    // READ: items for the home To-Do list (your claims + EVERYONE rows on events you can access).
    public async Task<EventItemResult<IEnumerable<EventItem>>> GetItemsForHomeTodosAsync(string userId)
    {
        var response = await _eventItemRepository.GetAllAsync(
            selector: i => i,
            where: i =>
                i.IsActive &&
                (i.Assignments.Any(a =>
                    a.Status != AssignmentStatus.Removed &&
                    a.UserId == userId &&
                    (a.Status == AssignmentStatus.Assigned ||
                     a.Status == AssignmentStatus.SignedUp ||
                     a.Status == AssignmentStatus.Completed)) ||
                 (i.Event.ItemsTasksEnabled == true &&
                  (i.Event.Roles.Any(r =>
                      r.UserId == userId &&
                      !r.HiddenFromList &&
                      (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner)) ||
                   i.Event.Attendances.Any(a => a.UserId == userId && !a.HiddenFromList)) &&
                  i.Assignments.Any(a =>
                      a.Status != AssignmentStatus.Removed &&
                      a.AssigneeType == AssigneeType.Everyone))),
            sortBy: i => i.Event!.StartAt,
            includes: [x => x.Event, x => x.Assignments]);

        if (!response.Succeeded || response.Result == null)
            return new EventItemResult<IEnumerable<EventItem>>
            {
                Succeeded = false,
                StatusCode = response.StatusCode,
                ErrorMessage = response.ErrorMessage ?? "Could not load home to-do items."
            };

        var items = response.Result.Select(MapItemForHomeTodos);
        return new EventItemResult<IEnumerable<EventItem>> { Succeeded = true, StatusCode = 200, Result = items };
    }

    // **************************************************************************************************************************
    // UPDATE
    public async Task<EventItemResult> UpdateEventItemAsync(string userId, string eventId, string eventItemId, EditItemFormData formData)
    {
        if (formData == null)
            return new EventItemResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the event, roles, and assignments so people-needed edits can sync placeholder slots
        var fetchResponse = await _eventItemRepository.GetEntityAsync(
            e => e.Id == eventItemId && e.EventId == eventId,
            includeChains:
            [
                q => q.Include(i => i.Event).ThenInclude(e => e.Roles),
                q => q.Include(i => i.Assignments)
            ]);

        var eventItemEntity = fetchResponse.Result;
        if (!fetchResponse.Succeeded || eventItemEntity == null)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event item not found." };

        // Check if user is owner or co-owner
        bool isOwnerOrCoOwner = eventItemEntity.Event.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to edit this event item." };

        var previousPeopleNeeded = eventItemEntity.PeopleNeeded;
        formData.MapOnto(eventItemEntity);
        var newPeopleNeeded = ClampPeopleNeeded(formData.PeopleNeeded);
        eventItemEntity.PeopleNeeded = newPeopleNeeded;

        if (newPeopleNeeded != previousPeopleNeeded)
        {
            var syncError = SyncAssignmentSlotsForPeopleNeeded(eventItemEntity, newPeopleNeeded);
            if (syncError != null)
                return syncError;
        }

        eventItemEntity.OriginalPeopleNeeded = eventItemEntity.PeopleNeeded;
        eventItemEntity.UpdatedAt = DateTime.UtcNow;

        var result = await _eventItemRepository.UpdateAsync(eventItemEntity);
        return result.Succeeded
            ? new EventItemResult { Succeeded = true, StatusCode = 200 }
            : new EventItemResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };

    }

    // **************************************************************************************************************************
    // DELETE
    public async Task<EventItemResult> DeleteEventItemAsync(string userId, string eventId, string eventItemId)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the event and its roles
        var fetchResponse = await _eventItemRepository.GetEntityAsync
        (
            e => e.Id == eventItemId && e.EventId == eventId,
            includeChains:
            [ q => q.Include(i => i.Event)
                .ThenInclude(e => e.Roles)]
        );

        var eventItemEntity = fetchResponse.Result;
        if (!fetchResponse.Succeeded || eventItemEntity == null)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event item not found." };

        // Check if user is owner or co-owner
        bool isOwnerOrCoOwner = eventItemEntity.Event.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to delete this event item." };

        // Soft Delete: Flip the active switch to false to keep DB integrity
        eventItemEntity.IsActive = false;
        eventItemEntity.UpdatedAt = DateTime.UtcNow;

        // Update
        var result = await _eventItemRepository.UpdateAsync(eventItemEntity);
        return result.Succeeded
            ? new EventItemResult { Succeeded = true, StatusCode = 200 }
            : new EventItemResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // INTERACTION: Claim and Unclaim an item methods and helpers.
    // Generated with help from AI
    // **************************************************************************************************************************
    // Guest claims an open PERSON # slot
    public async Task<EventItemResult> ClaimItemAsync(string userId, string eventId, string eventItemId, string? assignmentId = null)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the item, assignments, event, and roles
        var fetchResponse = await _eventItemRepository.GetEntityAsync
        (
            i => i.Id == eventItemId && i.EventId == eventId && i.IsActive,
            includeChains:
            [
                q => q.Include(i => i.Assignments),
                q => q.Include(i => i.Event).ThenInclude(e => e.Roles),
                q => q.Include(i => i.Event).ThenInclude(e => e.Attendances),
                q => q.Include(i => i.Event).ThenInclude(e => e.Items)
            ]
        );

        var item = fetchResponse.Result;
        if (!fetchResponse.Succeeded || item == null)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event item not found." };

        // Validate the guest interaction
        var permissionError = ValidateGuestItemInteraction(item, userId, requireOpenSlot: item.SignupMode == SignupMode.Hidden);
        if (permissionError != null)
            return permissionError;

        // Check if the item is assigned to everyone and cannot be claimed individually
        if (item.Assignments.Any(a => a.AssigneeType == AssigneeType.Everyone && a.Status != AssignmentStatus.Removed))
            return new EventItemResult { Succeeded = false, StatusCode = 409, ErrorMessage = "This item is assigned to everyone and cannot be claimed individually." };

        // Check if the user has already claimed a slot for this item
        if (item.Assignments.Any(a => IsActiveUserAssignment(a) && a.UserId == userId))
            return new EventItemResult { Succeeded = false, StatusCode = 409, ErrorMessage = "You have already claimed a slot for this item." };

        // Determine the number of slots needed and the number of active claims
        var slotsNeeded = ClampPeopleNeeded(item.PeopleNeeded);
        var activeClaims = item.Assignments.Count(IsActiveUserAssignment);
        // Resolve the open slot or create a new one
        var targetSlot = ResolveOpenSlot(item.Assignments, assignmentId);

        // If the target slot is not null, update the user assignment
        if (targetSlot != null)
        {
            targetSlot.UserId = userId;
            targetSlot.AssigneeType = AssigneeType.User;
            targetSlot.Status = AssignmentStatus.SignedUp;
            targetSlot.PlaceholderLabel = null;
            targetSlot.UpdatedAt = DateTime.UtcNow;
        }
        else if (activeClaims < slotsNeeded && !item.Assignments.Any(IsOpenSlot))
        {
            // Create a new user assignment
            item.Assignments.Add(new EventItemAssignmentEntity
            {
                Id = Guid.NewGuid().ToString(),
                EventItemId = eventItemId,
                UserId = userId,
                AssigneeType = AssigneeType.User,
                Status = AssignmentStatus.SignedUp,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
            return new EventItemResult { Succeeded = false, StatusCode = 409, ErrorMessage = "All slots for this item are currently full." };

        // If there are no open slots, disable the signup mode
        if (!item.Assignments.Any(IsOpenSlot))
            item.SignupMode = SignupMode.Disabled;

        // Update the item
        item.UpdatedAt = DateTime.UtcNow;
        var result = await _eventItemRepository.UpdateAsync(item);

        return result.Succeeded
            ? new EventItemResult { Succeeded = true, StatusCode = 200 }
            : new EventItemResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    //  Guest unclaims — slot reverts in place to PERSON #
    public async Task<EventItemResult> UnclaimItemAsync(string userId, string eventId, string eventItemId, string? assignmentId = null)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the item and assignments
        var fetchResponse = await _eventItemRepository.GetEntityAsync(
            i => i.Id == eventItemId && i.EventId == eventId && i.IsActive,
            includes: [x => x.Assignments]);

        var item = fetchResponse.Result;
        if (!fetchResponse.Succeeded || item == null)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event item not found." };

        // Resolve the user assignment
        var userAssignment = ResolveUserAssignment(item.Assignments, userId, assignmentId);
        if (userAssignment == null)
            return new EventItemResult { Succeeded = false, StatusCode = 400, ErrorMessage = "You do not have an active claim on this item." };

        // Check if there are other active users
        var otherActiveUsers = item.Assignments.Any(a =>
            a.Id != userAssignment.Id && IsActiveUserAssignment(a));

        // Update the user assignment
        userAssignment.UserId = null;
        userAssignment.AssigneeType = AssigneeType.OpenSlot;
        userAssignment.Status = AssignmentStatus.Assigned;
        userAssignment.PlaceholderLabel = $"PERSON {NextPersonSlotNumber(item.Assignments)}";
        userAssignment.UpdatedAt = DateTime.UtcNow;

        // If there are no other active users, revert the item to the original state
        if (!otherActiveUsers)
        {
            item.Title = item.OriginalTitle;
            item.Amount = item.OriginalAmount;
            item.PeopleNeeded = item.OriginalPeopleNeeded;
        }

        item.SignupMode = item.OriginalSignupMode;
        item.UpdatedAt = DateTime.UtcNow;

        // Update the item
        var result = await _eventItemRepository.UpdateAsync(item);

        return result.Succeeded
            ? new EventItemResult { Succeeded = true, StatusCode = 200 }
            : new EventItemResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // Toggles the signed-in user's completion flag for a home to-do bring line (persists to assignment status).
    public async Task<EventItemResult<bool>> ToggleItemCompletionAsync(string userId, string eventId, string eventItemId)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventItemResult<bool> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var fetchResponse = await _eventItemRepository.GetEntityAsync(
            i => i.Id == eventItemId && i.EventId == eventId && i.IsActive,
            includes: [x => x.Assignments]);

        var item = fetchResponse.Result;
        if (!fetchResponse.Succeeded || item == null)
            return new EventItemResult<bool> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event item not found." };

        if (!CanUserToggleHomeTodoItem(item.Assignments, userId))
            return new EventItemResult<bool> { Succeeded = false, StatusCode = 403, ErrorMessage = "You cannot update this item." };

        var assignment = ResolveUserTodoAssignment(item.Assignments, userId);
        var hasEveryone = item.Assignments.Any(a =>
            a.AssigneeType == AssigneeType.Everyone && a.Status != AssignmentStatus.Removed);

        bool nowDone;
        if (assignment == null)
        {
            if (!hasEveryone)
                return new EventItemResult<bool>
                {
                    Succeeded = false,
                    StatusCode = 400,
                    ErrorMessage = "Claim this item before marking it done.",
                };

            item.Assignments.Add(new EventItemAssignmentEntity
            {
                Id = Guid.NewGuid().ToString(),
                EventItemId = eventItemId,
                UserId = userId,
                AssigneeType = AssigneeType.User,
                Status = AssignmentStatus.Completed,
                CreatedAt = DateTime.UtcNow,
            });
            nowDone = true;
        }
        else if (assignment.Status == AssignmentStatus.Completed)
        {
            assignment.Status = hasEveryone ? AssignmentStatus.Removed : AssignmentStatus.SignedUp;
            assignment.UpdatedAt = DateTime.UtcNow;
            nowDone = false;
        }
        else if (assignment.Status is AssignmentStatus.SignedUp or AssignmentStatus.Assigned)
        {
            assignment.Status = AssignmentStatus.Completed;
            assignment.UpdatedAt = DateTime.UtcNow;
            nowDone = true;
        }
        else
            return new EventItemResult<bool> { Succeeded = false, StatusCode = 400, ErrorMessage = "This item cannot be marked done." };

        item.UpdatedAt = DateTime.UtcNow;
        var result = await _eventItemRepository.UpdateAsync(item);

        return result.Succeeded
            ? new EventItemResult<bool> { Succeeded = true, StatusCode = 200, Result = nowDone }
            : new EventItemResult<bool> { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // Keeps open PERSON # placeholders in sync when an organizer changes people needed on edit.
    private static EventItemResult? SyncAssignmentSlotsForPeopleNeeded(EventItemEntity item, int newPeopleNeeded)
    {
        if (item.Assignments.Any(a =>
                a.AssigneeType == AssigneeType.Everyone && a.Status != AssignmentStatus.Removed))
            return null;

        var activeAssignments = item.Assignments
            .Where(a => a.Status != AssignmentStatus.Removed)
            .ToList();

        var activeUserCount = activeAssignments.Count(IsActiveUserAssignment);
        var openSlots = activeAssignments.Where(IsOpenSlot).ToList();

        if (newPeopleNeeded < activeUserCount)
        {
            return new EventItemResult
            {
                Succeeded = false,
                StatusCode = 409,
                ErrorMessage =
                    $"Cannot set people needed below {activeUserCount} because that many people have already signed up."
            };
        }

        var currentCapacity = activeUserCount + openSlots.Count;
        var slotsToAdd = newPeopleNeeded - currentCapacity;

        if (slotsToAdd > 0)
        {
            for (var i = 0; i < slotsToAdd; i++)
            {
                var slotNumber = NextPersonSlotNumber(item.Assignments);
                item.Assignments.Add(CreateOpenSlotAssignment(item.Id, slotNumber));
            }

            if (item.SignupMode == SignupMode.Disabled)
            {
                item.SignupMode = item.OriginalSignupMode != SignupMode.Disabled
                    ? item.OriginalSignupMode
                    : SignupMode.Available;
            }
        }
        else if (slotsToAdd < 0)
        {
            var slotsToRemove = -slotsToAdd;
            if (openSlots.Count < slotsToRemove)
            {
                return new EventItemResult
                {
                    Succeeded = false,
                    StatusCode = 409,
                    ErrorMessage =
                        "Cannot reduce people needed: not enough open slots. Ask someone to unclaim first."
                };
            }

            var removableSlots = openSlots
                .OrderByDescending(a =>
                {
                    var match = Regex.Match(a.PlaceholderLabel ?? string.Empty, @"^PERSON\s+(\d+)$",
                        RegexOptions.IgnoreCase);
                    return match.Success && int.TryParse(match.Groups[1].Value, out var n) ? n : 0;
                })
                .Take(slotsToRemove);

            foreach (var slot in removableSlots)
            {
                slot.Status = AssignmentStatus.Removed;
                slot.UpdatedAt = DateTime.UtcNow;
            }
        }

        return null;
    }

    // **************************************************************************************************************************
    // Helpers: slot / assignment rules shared by claim and unclaim
    // **************************************************************************************************************************
    // Enforces boundary limits on the number of requested task slots for an item. Minimum of 1 and a maximum of 99.
    private static int ClampPeopleNeeded(int peopleNeeded)
    {
        return peopleNeeded < 1 ? 1 : Math.Min(peopleNeeded, 99);
    }

    // **************************************************************************************************************************
   // Determines if an assignment represents an active claim by a real volunteer.
   // Filters out empty placeholders and removed/soft-deleted historical data.
    private static bool IsActiveUserAssignment(EventItemAssignmentEntity assignment)
    {
        return !string.IsNullOrEmpty(assignment.UserId) &&
            assignment.Status is AssignmentStatus.SignedUp or AssignmentStatus.Assigned;
    }

    // **************************************************************************************************************************
    // Evaluates if an assignment is a valid, available placeholder slot that a guest can volunteer for.
    private static bool IsOpenSlot(EventItemAssignmentEntity assignment)
    {
        return assignment.AssigneeType == AssigneeType.OpenSlot &&
            string.IsNullOrEmpty(assignment.UserId) &&
            assignment.Status != AssignmentStatus.Removed;
    }

    // **************************************************************************************************************************
    // Calculates the next logically available "PERSON #" label by scanning existing assignments using Regex.
    // Automatically fills in gaps (e.g., if Person 2 cancels, the next slot generated is 2, not 4).
    private static int NextPersonSlotNumber(ICollection<EventItemAssignmentEntity> assignments)
    {
        var usedNumbers = assignments
            .Select(a => a.PlaceholderLabel)
            .Select(label =>
            {
                var match = Regex.Match(label ?? string.Empty, @"^PERSON\s+(\d+)$", RegexOptions.IgnoreCase);
                return match.Success && int.TryParse(match.Groups[1].Value, out var n) ? n : (int?)null;
            })
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToHashSet();

        for (var i = 1; i <= 99; i++)
        {
            if (!usedNumbers.Contains(i))
                return i;
        }

        return 99;
    }

    // **************************************************************************************************************************
    // Locates an available slot for a guest to claim.
    // Prioritizes a specific slot ID if requested by the UI, otherwise falls back to the first available open slot.
    private static EventItemAssignmentEntity? ResolveOpenSlot(ICollection<EventItemAssignmentEntity> assignments, string? assignmentId)
    {
        if (!string.IsNullOrEmpty(assignmentId))
        {
            var specific = assignments.FirstOrDefault(a => a.Id == assignmentId);
            return specific != null && IsOpenSlot(specific) ? specific : null;
        }

        return assignments.FirstOrDefault(IsOpenSlot);
    }

    // **************************************************************************************************************************
    // Locates a specific user's active volunteer assignment.
    // Useful for unclaiming tasks or modifying an existing claim.
    private static EventItemAssignmentEntity? ResolveUserAssignment(ICollection<EventItemAssignmentEntity> assignments, string userId, string? assignmentId)
    {
        if (!string.IsNullOrEmpty(assignmentId))
        {
            return assignments.FirstOrDefault(a =>
                a.Id == assignmentId && a.UserId == userId && IsActiveUserAssignment(a));
        }

        return assignments.FirstOrDefault(a => a.UserId == userId && IsActiveUserAssignment(a));
    }

    // **************************************************************************************************************************
    private static EventItemAssignmentEntity? ResolveUserTodoAssignment( ICollection<EventItemAssignmentEntity> assignments, string userId)
    {
        return assignments.FirstOrDefault(a =>
            a.UserId == userId &&
            a.Status != AssignmentStatus.Removed &&
            a.AssigneeType == AssigneeType.User &&
            a.Status is AssignmentStatus.SignedUp
                or AssignmentStatus.Assigned
                or AssignmentStatus.Completed);
    }

    // **************************************************************************************************************************
    private static bool CanUserToggleHomeTodoItem(ICollection<EventItemAssignmentEntity> assignments, string userId)
    {
        return assignments.Any(a =>
            a.Status != AssignmentStatus.Removed &&
            ((a.UserId == userId &&
              a.AssigneeType == AssigneeType.User &&
              a.Status is AssignmentStatus.SignedUp
                  or AssignmentStatus.Assigned
                  or AssignmentStatus.Completed) ||
             a.AssigneeType == AssigneeType.Everyone));
    }

    // **************************************************************************************************************************
    // Generates a new database entity for an open placeholder slot, automatically applying the "PERSON #" label.
    private static EventItemAssignmentEntity CreateOpenSlotAssignment(string eventItemId, int personNumber)
    {
        return new()
        {
            Id = Guid.NewGuid().ToString(),
            EventItemId = eventItemId,
            AssigneeType = AssigneeType.OpenSlot,
            PlaceholderLabel = $"PERSON {personNumber}",
            Status = AssignmentStatus.Assigned,
            CreatedAt = DateTime.UtcNow
        };
    }

    // **************************************************************************************************************************
    // Owners/co-owners may always add items; guests may add when the event allows it and they have accepted.
    // Generated with help from AI
    private static EventItemResult? ValidateCreateItemPermission(EventEntity fetchEvent, string userId)
    {
        var isOwnerOrCoOwner = fetchEvent.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (isOwnerOrCoOwner)
            return null;

        if (fetchEvent.AllowGuestBringItems != true)
            return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to create an item for this event." };

        var hasAcceptedAttendance = fetchEvent.Attendances.Any(a =>
            a.UserId == userId && a.Status == AttendanceStatus.Accepted);

        if (!hasAcceptedAttendance)
            return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You must join the event before adding an item." };

        return null;
    }

    // **************************************************************************************************************************
    // Enforces the core business rules for guest interactions. Validates event role permissions,
    // event-level availability, and item-level signup visibility.
    private static EventItemResult? ValidateGuestItemInteraction(EventItemEntity item, string userId, bool requireOpenSlot)
    {
        // Check if the user is an owner or co-owner
        var isOwnerOrCoOwner = item.Event.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
        {
            // Guests may sign up for items even if they declined or haven't responded yet.
            // Access is already enforced by `VerifyViewAccessAsync` earlier in the call chain.
            var eventHasClaimableItems = item.Event.Items.Any(i => i.IsActive);
            if (!eventHasClaimableItems)
                return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "There are no items to claim for this event." };
        }

        //  Check if the host manually disabled signups for this specific item
        if (item.SignupMode == SignupMode.Disabled && !isOwnerOrCoOwner)
            return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "Signup is disabled for this item." };

        // Prevent guests from trying to claim hidden/full items
        if (requireOpenSlot && !item.Assignments.Any(IsOpenSlot) && !isOwnerOrCoOwner)
            return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "Signup is hidden until a slot becomes available." };

        return null;
    }

    // **************************************************************************************************************************
    private static EventItem MapItemForHomeTodos(EventItemEntity entity)
    {
        var item = entity.MapTo<EventItem>();
        item.Assignments = entity.Assignments
            .Select(a => a.MapTo<EventItemAssignment>())
            .ToList();

        if (entity.Event != null)
        {
            item.EventTitle = entity.Event.Title;
            item.EventSlug = entity.Event.Slug;
            item.EventStartAt = entity.Event.StartAt;
            item.EventListLocation = FormatEventListLocation(entity.Event);
        }

        return item;
    }

    // **************************************************************************************************************************
    private static string? FormatEventListLocation(EventEntity ev)
    {
        return !string.IsNullOrWhiteSpace(ev.LocationName) ? ev.LocationName : ev.LocationStreet;
    }
}
