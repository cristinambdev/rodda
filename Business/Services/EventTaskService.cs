using System.Text.RegularExpressions;
using Business.Dtos;
using Data.Entities;
using Data.Repositories;
using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public interface IEventTaskService
{
    Task<EventTaskResult> CreateEventTaskAsync(string userId, AddTaskFormData formData);
    Task<EventTaskResult> DeleteEventTaskAsync(string userId, string eventId, string eventTaskId);
    Task<EventTaskResult<EventTask>> GetEventTaskAsync(string eventId, string eventTaskId);
    Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksClaimedByUserAsync(string userId);
    Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksForHomeTodosAsync(string userId);
    Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksForEventAsync(string userId, string eventId);
    Task<EventTaskResult> UpdateEventTaskAsync(string userId, string eventId, string eventTaskId, EditTaskFormData formData);
    Task<EventTaskResult> ClaimTaskAsync(string userId, string eventId, string eventTaskId, string? assignmentId = null);
    Task<EventTaskResult> UnclaimTaskAsync(string userId, string eventId, string eventTaskId, string? assignmentId = null);
    Task<EventTaskResult<bool>> ToggleTaskCompletionAsync(string userId, string eventId, string eventTaskId);
}

// Handles EventTaskEntity, EventTaskAssignmentEntity

public class EventTaskService( IEventTaskRepository eventTaskRepository, IEventTaskAssignmentRepository eventTaskAssignmentRepository, IEventRepository eventRepository, IEventAccessService eventAccessService) : IEventTaskService
{
    private readonly IEventTaskRepository _eventTaskRepository = eventTaskRepository;
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IEventTaskAssignmentRepository _eventTaskAssignmentRepository = eventTaskAssignmentRepository;
    private readonly IEventAccessService _eventAccessService = eventAccessService;

    // **************************************************************************************************************************
    // CREATE
    public async Task<EventTaskResult> CreateEventTaskAsync(string userId, AddTaskFormData formData)
    {
        if (formData == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        // Fetch the event with roles and attendances to check permissions
        var fetchResponse = await _eventRepository.GetEntityAsync
        (
            e => e.Id == formData.EventId,
            includes: [x => x.Roles, x => x.Attendances]
        );

        var fetchEvent = fetchResponse.Result;
        if (!fetchResponse.Succeeded || fetchEvent == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Event with id {formData.EventId} was not found" };

        if (!_eventAccessService.HasViewAccess(fetchEvent, userId))
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var permissionError = ValidateCreateTaskPermission(fetchEvent, userId);
        if (permissionError != null)
            return permissionError;

        var displayTitle = (formData.Title ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(displayTitle))
            return new EventTaskResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Task name is required." };

        var newEventTask = formData.MapTo<EventTaskEntity>();
        newEventTask.EventId = formData.EventId;
        newEventTask.Title = displayTitle;
        newEventTask.CreatedByUserId = userId;
        newEventTask.IsActive = true;
        newEventTask.CreatedAt = DateTime.UtcNow;

        var peopleNeeded = ClampPeopleNeeded(formData.PeopleNeeded);
        newEventTask.PeopleNeeded = peopleNeeded;
        newEventTask.OriginalTitle = displayTitle;
        newEventTask.OriginalScheduledAt = formData.ScheduledAt;
        newEventTask.OriginalTaskTime = formData.TaskTime;
        newEventTask.OriginalTaskLocation = formData.TaskLocation;
        newEventTask.OriginalPeopleNeeded = peopleNeeded;
        newEventTask.OriginalSignupMode = formData.SignupMode;

        var result = await _eventTaskRepository.AddAsync(newEventTask);
        if (!result.Succeeded)
            return new EventTaskResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };

        for (var slot = 1; slot <= peopleNeeded; slot++)
        {
            var slotResult = await _eventTaskAssignmentRepository.AddAsync(
                CreateOpenSlotAssignment(newEventTask.Id, slot));
            if (!slotResult.Succeeded)
                return new EventTaskResult { Succeeded = false, StatusCode = slotResult.StatusCode, ErrorMessage = slotResult.ErrorMessage };
        }

        return new EventTaskResult { Succeeded = true, StatusCode = 201 };
    }

    // **************************************************************************************************************************
    // READ
    public async Task<EventTaskResult<EventTask>> GetEventTaskAsync(string eventId, string eventTaskId)
    {
        // Query the repository for a matching task belonging to the specified event, including its current claims
        var response = await _eventTaskRepository.GetAsync(
            where: t => t.Id == eventTaskId && t.EventId == eventId,
            includes: [x => x.Assignments]);

        return response.Succeeded
            ? new EventTaskResult<EventTask> { Succeeded = true, StatusCode = 200, Result = response.Result }
            : new EventTaskResult<EventTask> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };
    }

    // **************************************************************************************************************************
    // READ: Get all tasks for an event page
    public async Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksForEventAsync(string userId, string eventId)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventTaskResult<IEnumerable<EventTask>> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Grab only active (non-soft-deleted) tasks for this event, complete with user assignments
        var response = await _eventTaskRepository.GetAllAsync(
            selector: t => t,
            where: t => t.EventId == eventId && t.IsActive,
            sortBy: t => t.SortOrder,
            includeChains:
            [
                q => q.Include(t => t.Assignments).ThenInclude(a => a.User),
                q => q.Include(t => t.CreatedByUser)
            ]);

        if (!response.Succeeded || response.Result == null)
            return new EventTaskResult<IEnumerable<EventTask>>
            {
                Succeeded = false,
                StatusCode = response.StatusCode,
                ErrorMessage = response.ErrorMessage ?? "Could not load event tasks."
            };

        var tasks = response.Result.Select(entity =>
        {
            var task = entity.MapTo<EventTask>();
            task.Assignments = entity.Assignments.Select(a =>
            {
                var assignment = a.MapTo<EventTaskAssignment>();
                assignment.User = a.User?.MapTo<User>();
                return assignment;
            }).ToList();
            task.CreatedByUser = entity.CreatedByUser?.MapTo<User>();
            return task;
        });

        return new EventTaskResult<IEnumerable<EventTask>> { Succeeded = true, StatusCode = 200, Result = tasks };
    }

    // **************************************************************************************************************************
    // READ: Get all tasks claimed by a specific user for their To-Do Dashboard
    public async Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksClaimedByUserAsync(string userId)
    {
        // Extract tasks where the user is actively locked into an 'Assigned' or 'SignedUp' tracking status
        var response = await _eventTaskRepository.GetAllAsync(
            selector: t => t,
            where: t => t.IsActive && t.Assignments.Any(a =>
                a.UserId == userId &&
                (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.SignedUp)),
            sortBy: t => t.CreatedAt,
            includes: [x => x.Event, x => x.Assignments]);

        if (!response.Succeeded || response.Result == null)
            return new EventTaskResult<IEnumerable<EventTask>>
            {
                Succeeded = false,
                StatusCode = response.StatusCode,
                ErrorMessage = response.ErrorMessage ?? "Could not load claimed tasks."
            };

        // Map the structural model and manually bind the unmapped parent event title
        var tasks = response.Result.Select(entity =>
        {
            var task = entity.MapTo<EventTask>();
            task.EventTitle = entity.Event?.Title;
            return task;
        });

        return new EventTaskResult<IEnumerable<EventTask>> { Succeeded = true, StatusCode = 200, Result = tasks };
    }

    // **************************************************************************************************************************
    // READ: tasks for the home To-Do list (your claims + EVERYONE rows on events you can access).
    public async Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksForHomeTodosAsync(string userId)
    {
        var response = await _eventTaskRepository.GetAllAsync(
            selector: t => t,
            where: t =>
                t.IsActive &&
                (t.Assignments.Any(a =>
                    a.Status != AssignmentStatus.Removed &&
                    a.UserId == userId &&
                    (a.Status == AssignmentStatus.Assigned ||
                     a.Status == AssignmentStatus.SignedUp ||
                     a.Status == AssignmentStatus.Completed)) ||
                 (t.Event.ItemsTasksEnabled == true &&
                  (t.Event.Roles.Any(r =>
                      r.UserId == userId &&
                      !r.HiddenFromList &&
                      (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner)) ||
                   t.Event.Attendances.Any(a => a.UserId == userId && !a.HiddenFromList)) &&
                  t.Assignments.Any(a =>
                      a.Status != AssignmentStatus.Removed &&
                      a.AssigneeType == AssigneeType.Everyone))),
            sortBy: t => t.Event!.StartAt,
            includes: [x => x.Event, x => x.Assignments]);

        if (!response.Succeeded || response.Result == null)
            return new EventTaskResult<IEnumerable<EventTask>>
            {
                Succeeded = false,
                StatusCode = response.StatusCode,
                ErrorMessage = response.ErrorMessage ?? "Could not load home to-do tasks."
            };

        var tasks = response.Result.Select(MapTaskForHomeTodos);
        return new EventTaskResult<IEnumerable<EventTask>> { Succeeded = true, StatusCode = 200, Result = tasks };
    }

    // **************************************************************************************************************************
    // UPDATE
    public async Task<EventTaskResult> UpdateEventTaskAsync(string userId, string eventId, string eventTaskId, EditTaskFormData formData)
    {
        if (formData == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the event, roles, and assignments so people-needed edits can sync placeholder slots
        var fetchResponse = await _eventTaskRepository.GetEntityAsync(
            t => t.Id == eventTaskId && t.EventId == eventId,
            includeChains:
            [
                q => q.Include(t => t.Event).ThenInclude(e => e.Roles),
                q => q.Include(t => t.Assignments)
            ]);

        var eventTaskEntity = fetchResponse.Result;
        if (!fetchResponse.Succeeded || eventTaskEntity == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };

        // Check if user is owner or co-owner
        bool isOwnerOrCoOwner = eventTaskEntity.Event.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to edit this event task." };

        var displayTitle = (formData.Title ?? "").Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(displayTitle))
            return new EventTaskResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Task name is required." };

        formData.Title = displayTitle;
        var previousPeopleNeeded = eventTaskEntity.PeopleNeeded;
        formData.MapOnto(eventTaskEntity);
        eventTaskEntity.OriginalTitle = displayTitle;
        var newPeopleNeeded = ClampPeopleNeeded(formData.PeopleNeeded);
        eventTaskEntity.PeopleNeeded = newPeopleNeeded;

        if (newPeopleNeeded != previousPeopleNeeded)
        {
            var syncError = SyncAssignmentSlotsForPeopleNeeded(eventTaskEntity, newPeopleNeeded);
            if (syncError != null)
                return syncError;
        }

        eventTaskEntity.OriginalPeopleNeeded = eventTaskEntity.PeopleNeeded;
        eventTaskEntity.UpdatedAt = DateTime.UtcNow;

        var result = await _eventTaskRepository.UpdateAsync(eventTaskEntity);
        return result.Succeeded
            ? new EventTaskResult { Succeeded = true, StatusCode = 200 }
            : new EventTaskResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // DELETE
    public async Task<EventTaskResult> DeleteEventTaskAsync(string userId, string eventId, string eventTaskId)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the event and its roles
        var fetchResponse = await _eventTaskRepository.GetEntityAsync(
            t => t.Id == eventTaskId && t.EventId == eventId,
            includeChains:
            [
                q => q.Include(t => t.Event).ThenInclude(e => e.Roles)
            ]);

        var eventTaskEntity = fetchResponse.Result;
        if (!fetchResponse.Succeeded || eventTaskEntity == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };

        // Check if user is owner or co-owner
        bool isOwnerOrCoOwner = eventTaskEntity.Event.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to delete this event task." };

        // Soft Delete: Flip the active switch to false to keep DB integrity
        eventTaskEntity.IsActive = false;
        eventTaskEntity.UpdatedAt = DateTime.UtcNow;

        // Update
        var result = await _eventTaskRepository.UpdateAsync(eventTaskEntity);
        return result.Succeeded
            ? new EventTaskResult { Succeeded = true, StatusCode = 200 }
            : new EventTaskResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // INTERACTION: Claim and Unclaim a task methods and helpers.
    // Generated with help from AI
    // **************************************************************************************************************************
    // Guest claims an open PERSON # slot for a task
    public async Task<EventTaskResult> ClaimTaskAsync(string userId, string eventId, string eventTaskId, string? assignmentId = null)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the task, assignments, event, and roles
        var fetchResponse = await _eventTaskRepository.GetEntityAsync(
            t => t.Id == eventTaskId && t.EventId == eventId && t.IsActive,
            includeChains:
            [
                q => q.Include(t => t.Assignments),
                q => q.Include(t => t.Event).ThenInclude(e => e.Roles),
                q => q.Include(t => t.Event).ThenInclude(e => e.Attendances),
                q => q.Include(t => t.Event).ThenInclude(e => e.Tasks)
            ]);

        var task = fetchResponse.Result;
        if (!fetchResponse.Succeeded || task == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };

        // Validate the guest interaction
        var permissionError = ValidateGuestTaskInteraction(task, userId, requireOpenSlot: task.SignupMode == SignupMode.Hidden);
        if (permissionError != null)
            return permissionError;

        // Check if the task is assigned to everyone and cannot be claimed individually
        if (task.Assignments.Any(a => a.AssigneeType == AssigneeType.Everyone && a.Status != AssignmentStatus.Removed))
            return new EventTaskResult { Succeeded = false, StatusCode = 409, ErrorMessage = "This task is assigned to everyone and cannot be claimed individually." };

        // Check if the user has already claimed a slot for this task
        if (task.Assignments.Any(a => IsActiveUserAssignment(a) && a.UserId == userId))
            return new EventTaskResult { Succeeded = false, StatusCode = 409, ErrorMessage = "You have already claimed this task." };

        // Determine the number of slots needed and the number of active claims
        var slotsNeeded = ClampPeopleNeeded(task.PeopleNeeded);
        var activeClaims = task.Assignments.Count(IsActiveUserAssignment);
        // Resolve the open slot or create a new one
        var targetSlot = ResolveOpenSlot(task.Assignments, assignmentId);

        // If the target slot is not null, update the user assignment
        if (targetSlot != null)
        {
            targetSlot.UserId = userId;
            targetSlot.AssigneeType = AssigneeType.User;
            targetSlot.Status = AssignmentStatus.SignedUp;
            targetSlot.PlaceholderLabel = null;
            targetSlot.UpdatedAt = DateTime.UtcNow;
        }
        else if (activeClaims < slotsNeeded && !task.Assignments.Any(IsOpenSlot))
        {
            // Create a new user assignment
            task.Assignments.Add(new EventTaskAssignmentEntity
            {
                Id = Guid.NewGuid().ToString(),
                EventTaskId = eventTaskId,
                UserId = userId,
                AssigneeType = AssigneeType.User,
                Status = AssignmentStatus.SignedUp,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
            return new EventTaskResult { Succeeded = false, StatusCode = 409, ErrorMessage = "All slots for this task are currently full." };

        // If there are no open slots, disable the signup mode
        if (!task.Assignments.Any(IsOpenSlot))
            task.SignupMode = SignupMode.Disabled;

        // Update the task
        task.UpdatedAt = DateTime.UtcNow;
        var result = await _eventTaskRepository.UpdateAsync(task);

        return result.Succeeded
            ? new EventTaskResult { Succeeded = true, StatusCode = 200 }
            : new EventTaskResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    //  Guest unclaims — slot reverts in place to PERSON #
    public async Task<EventTaskResult> UnclaimTaskAsync(string userId, string eventId, string eventTaskId, string? assignmentId = null)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Include the task and assignments
        var fetchResponse = await _eventTaskRepository.GetEntityAsync(
            t => t.Id == eventTaskId && t.EventId == eventId && t.IsActive,
            includes: [x => x.Assignments]);

        var task = fetchResponse.Result;
        if (!fetchResponse.Succeeded || task == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };

        // Resolve the user assignment
        var userAssignment = ResolveUserAssignment(task.Assignments, userId, assignmentId);
        if (userAssignment == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 400, ErrorMessage = "You do not have an active claim on this task." };

        // Check if there are other active users
        var otherActiveUsers = task.Assignments.Any(a =>
            a.Id != userAssignment.Id && IsActiveUserAssignment(a));

        // Update the user assignment
        userAssignment.UserId = null;
        userAssignment.AssigneeType = AssigneeType.OpenSlot;
        userAssignment.Status = AssignmentStatus.Assigned;
        userAssignment.PlaceholderLabel = $"PERSON {NextPersonSlotNumber(task.Assignments)}";
        userAssignment.UpdatedAt = DateTime.UtcNow;

        // If there are no other active users, revert the task to the original state
        if (!otherActiveUsers)
            RevertDisplayFieldsFromOriginal(task);

        task.SignupMode = task.OriginalSignupMode;
        task.UpdatedAt = DateTime.UtcNow;

        // Update the task
        var result = await _eventTaskRepository.UpdateAsync(task);

        return result.Succeeded
            ? new EventTaskResult { Succeeded = true, StatusCode = 200 }
            : new EventTaskResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // Toggles the signed-in user's completion flag for a home to-do task line (persists to assignment status).
    public async Task<EventTaskResult<bool>> ToggleTaskCompletionAsync(string userId, string eventId, string eventTaskId)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventTaskResult<bool> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var fetchResponse = await _eventTaskRepository.GetEntityAsync(
            t => t.Id == eventTaskId && t.EventId == eventId && t.IsActive,
            includes: [x => x.Assignments]);

        var task = fetchResponse.Result;
        if (!fetchResponse.Succeeded || task == null)
            return new EventTaskResult<bool> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };

        if (!CanUserToggleHomeTodoTask(task.Assignments, userId))
            return new EventTaskResult<bool> { Succeeded = false, StatusCode = 403, ErrorMessage = "You cannot update this task." };

        var assignment = ResolveUserTodoAssignment(task.Assignments, userId);
        var hasEveryone = task.Assignments.Any(a =>
            a.AssigneeType == AssigneeType.Everyone && a.Status != AssignmentStatus.Removed);

        bool nowDone;
        if (assignment == null)
        {
            if (!hasEveryone)
                return new EventTaskResult<bool>
                {
                    Succeeded = false,
                    StatusCode = 400,
                    ErrorMessage = "Claim this task before marking it done.",
                };

            task.Assignments.Add(new EventTaskAssignmentEntity
            {
                Id = Guid.NewGuid().ToString(),
                EventTaskId = eventTaskId,
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
            return new EventTaskResult<bool> { Succeeded = false, StatusCode = 400, ErrorMessage = "This task cannot be marked done." };

        task.UpdatedAt = DateTime.UtcNow;
        var result = await _eventTaskRepository.UpdateAsync(task);

        return result.Succeeded
            ? new EventTaskResult<bool> { Succeeded = true, StatusCode = 200, Result = nowDone }
            : new EventTaskResult<bool> { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // Keeps open slot placeholders in sync when an organizer changes people needed on edit.
    private static EventTaskResult? SyncAssignmentSlotsForPeopleNeeded(EventTaskEntity task, int newPeopleNeeded)
    {
        if (task.Assignments.Any(a =>
                a.AssigneeType == AssigneeType.Everyone && a.Status != AssignmentStatus.Removed))
            return null;

        var activeAssignments = task.Assignments
            .Where(a => a.Status != AssignmentStatus.Removed)
            .ToList();

        var activeUserCount = activeAssignments.Count(IsActiveUserAssignment);
        var openSlots = activeAssignments.Where(IsOpenSlot).ToList();

        if (newPeopleNeeded < activeUserCount)
        {
            return new EventTaskResult
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
                var slotNumber = NextPersonSlotNumber(task.Assignments);
                task.Assignments.Add(CreateOpenSlotAssignment(task.Id, slotNumber));
            }

            if (task.SignupMode == SignupMode.Disabled)
            {
                task.SignupMode = task.OriginalSignupMode != SignupMode.Disabled
                    ? task.OriginalSignupMode
                    : SignupMode.Available;
            }
        }
        else if (slotsToAdd < 0)
        {
            var slotsToRemove = -slotsToAdd;
            if (openSlots.Count < slotsToRemove)
            {
                return new EventTaskResult
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
    // Enforces boundary limits on the number of requested task slots for a task. Minimum of 1 and a maximum of 99.
    private static int ClampPeopleNeeded(int peopleNeeded)
    {
        return peopleNeeded < 1 ? 1 : Math.Min(peopleNeeded, 99);
    }

    // **************************************************************************************************************************
    // Determines if an assignment represents an active claim by a real volunteer.
    // Filters out empty placeholders and removed/soft-deleted historical data.
    private static bool IsActiveUserAssignment(EventTaskAssignmentEntity assignment)
    {
        return !string.IsNullOrEmpty(assignment.UserId) &&
            assignment.Status is AssignmentStatus.SignedUp or AssignmentStatus.Assigned;
    }

    // **************************************************************************************************************************
    // Evaluates if an assignment is a valid, available placeholder slot that a guest can volunteer for.
    private static bool IsOpenSlot(EventTaskAssignmentEntity assignment)
    {
        return assignment.AssigneeType == AssigneeType.OpenSlot &&
            string.IsNullOrEmpty(assignment.UserId) &&
            assignment.Status != AssignmentStatus.Removed;
    }

    // **************************************************************************************************************************
    // Calculates the next logically available "PERSON #" label by scanning existing assignments using Regex.
    // Automatically fills in gaps (e.g., if Person 2 cancels, the next slot generated is 2, not 4).
    private static int NextPersonSlotNumber(ICollection<EventTaskAssignmentEntity> assignments)
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
    private static EventTaskAssignmentEntity? ResolveOpenSlot(
        ICollection<EventTaskAssignmentEntity> assignments,
        string? assignmentId)
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
    private static EventTaskAssignmentEntity? ResolveUserAssignment(
        ICollection<EventTaskAssignmentEntity> assignments,
        string userId,
        string? assignmentId)
    {
        if (!string.IsNullOrEmpty(assignmentId))
        {
            return assignments.FirstOrDefault(a =>
                a.Id == assignmentId && a.UserId == userId && IsActiveUserAssignment(a));
        }

        return assignments.FirstOrDefault(a => a.UserId == userId && IsActiveUserAssignment(a));
    }

    // **************************************************************************************************************************
    private static EventTaskAssignmentEntity? ResolveUserTodoAssignment(ICollection<EventTaskAssignmentEntity> assignments, string userId)
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
    private static bool CanUserToggleHomeTodoTask(ICollection<EventTaskAssignmentEntity> assignments, string userId)
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
    private static EventTaskAssignmentEntity CreateOpenSlotAssignment(string eventTaskId, int personNumber)
    {
        return new EventTaskAssignmentEntity
        {
            Id = Guid.NewGuid().ToString(),
            EventTaskId = eventTaskId,
            AssigneeType = AssigneeType.OpenSlot,
            PlaceholderLabel = $"PERSON {personNumber}",
            Status = AssignmentStatus.Assigned,
            CreatedAt = DateTime.UtcNow
        };
    }

    // **************************************************************************************************************************
    // Reverts display fields on the task when the last assignee leaves their slot.
    private static void RevertDisplayFieldsFromOriginal(EventTaskEntity task)
    {
        task.Title = task.OriginalTitle;
        task.ScheduledAt = task.OriginalScheduledAt;
        task.TaskTime = task.OriginalTaskTime;
        task.TaskLocation = task.OriginalTaskLocation;
        task.PeopleNeeded = task.OriginalPeopleNeeded;
    }

    // **************************************************************************************************************************
    // Owners/co-owners may always add tasks; guests may add when the event allows it and they have accepted.
    private static EventTaskResult? ValidateCreateTaskPermission(EventEntity fetchEvent, string userId)
    {
        var isOwnerOrCoOwner = fetchEvent.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (isOwnerOrCoOwner)
            return null;

        if (fetchEvent.AllowGuestTasks != true)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to create a task for this event." };

        var hasAcceptedAttendance = fetchEvent.Attendances.Any(a =>
            a.UserId == userId && a.Status == AttendanceStatus.Accepted);

        if (!hasAcceptedAttendance)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You must join the event before adding a task." };

        return null;
    }

    // **************************************************************************************************************************
    // Enforces the core business rules for guest interactions. Validates event role permissions,
    // event-level availability, and task-level signup visibility.
    private static EventTaskResult? ValidateGuestTaskInteraction(EventTaskEntity task, string userId, bool requireOpenSlot)
    {
        // Check if the user is an owner or co-owner
        var isOwnerOrCoOwner = task.Event.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
        {
            // Guests may sign up for tasks even if they declined or haven't responded yet.
            // Access is already enforced by `VerifyViewAccessAsync` earlier in the call chain.
            var eventHasClaimableTasks = task.Event.Tasks.Any(t => t.IsActive);
            if (!eventHasClaimableTasks)
                return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "There are no tasks to claim for this event." };
        }

        //  Check if the host manually disabled signups for this specific task
        if (task.SignupMode == SignupMode.Disabled && !isOwnerOrCoOwner)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "Signup is disabled for this task." };

        // Prevent guests from trying to claim hidden/full tasks
        if (requireOpenSlot && !task.Assignments.Any(IsOpenSlot) && !isOwnerOrCoOwner)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "Signup is hidden until a slot becomes available." };

        return null;
    }

    // **************************************************************************************************************************
    private static EventTask MapTaskForHomeTodos(EventTaskEntity entity)
    {
        var task = entity.MapTo<EventTask>();
        task.Assignments = entity.Assignments
            .Select(a => a.MapTo<EventTaskAssignment>())
            .ToList();

        if (entity.Event != null)
        {
            task.EventTitle = entity.Event.Title;
            task.EventSlug = entity.Event.Slug;
            task.EventStartAt = entity.Event.StartAt;
            task.EventListLocation = FormatEventListLocation(entity.Event);
        }

        return task;
    }

    // **************************************************************************************************************************
    private static string? FormatEventListLocation(EventEntity ev) =>
        !string.IsNullOrWhiteSpace(ev.LocationName) ? ev.LocationName : ev.LocationStreet;
}
