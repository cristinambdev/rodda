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
    Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksForEventAsync(string eventId);
    Task<EventTaskResult> UpdateEventTaskAsync(string userId, string eventId, string eventTaskId, EditTaskFormData formData);
}

// Handles EventTaskEntity, EventTaskAssignmentEntity

public class EventTaskService(IEventTaskRepository eventTaskRepository, IEventTaskAssignmentRepository eventTaskAssignmentRepository, IEventRepository eventRepository) : IEventTaskService
{
    private readonly IEventTaskRepository _eventTaskRepository = eventTaskRepository;
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IEventTaskAssignmentRepository _eventTaskAssignmentRepository = eventTaskAssignmentRepository;

    // **************************************************************************************************************************
    // CREATE
    public async Task<EventTaskResult> CreateEventTaskAsync(string userId, AddTaskFormData formData)
    {
        if (formData == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        // Fetch the event with roles to check permissions
        var fetchResponse = await _eventRepository.GetEntityAsync
          (
            e => e.Id == formData.EventId,
            includes: [x => x.Roles]
          );

        var fetchEvent = fetchResponse.Result;
        if (!fetchResponse.Succeeded || fetchEvent == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Event with id {formData.EventId} was not found" };

        // Persmission check
        bool isOwnerOrCoOwner = fetchEvent.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to create a task for this event." };

        var newEventTask = formData.MapTo<EventTaskEntity>();

        newEventTask.CreatedByUserId = userId;
        newEventTask.IsActive = true;
        newEventTask.CreatedAt = DateTime.UtcNow;

        // Populate the original values
        newEventTask.OriginalTitle = formData.Title;
        newEventTask.OriginalScheduledAt = formData.ScheduledAt;
        newEventTask.OriginalTaskTime = formData.TaskTime;
        newEventTask.OriginalTaskLocation = formData.TaskLocation;
        newEventTask.OriginalSignupMode = formData.SignupMode;

        var result = await _eventTaskRepository.AddAsync(newEventTask);
        return result.Succeeded
            ? new EventTaskResult { Succeeded = true, StatusCode = 201 }
            : new EventTaskResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // READ
    public async Task<EventTaskResult<EventTask>> GetEventTaskAsync(string eventId, string eventTaskId)
    {
        // Query the repository for a matching item belonging to the specified event, including its current claims
        var response = await _eventTaskRepository.GetAsync
          (
            where: t => t.Id == eventTaskId && t.EventId == eventId,
            includes: [x => x.Assignments]
          );

        return response.Succeeded
            ? new EventTaskResult<EventTask> { Succeeded = true, StatusCode = 200, Result = response.Result }
            : new EventTaskResult<EventTask> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };
    }

    // **************************************************************************************************************************
    // READ: Get all tasks for an event page
    public async Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksForEventAsync(string eventId)
    {
        // Grab only active (non-soft-deleted) items for this event, complete with user assignments
        var response = await _eventTaskRepository.GetAllAsync
          (
            selector: t => t,
            where: t => t.EventId == eventId && t.IsActive,
            sortBy: t => t.SortOrder,
            includes: [x => x.Assignments]
          );

        if (!response.Succeeded || response.Result == null)
            return new EventTaskResult<IEnumerable<EventTask>>
            { Succeeded = false, StatusCode = response.StatusCode, ErrorMessage = response.ErrorMessage ?? "Could not load event tasks." };

        return new EventTaskResult<IEnumerable<EventTask>>
        { Succeeded = true, StatusCode = 200, Result = response.Result.MapTo<IEnumerable<EventTask>>() };
    }

    // **************************************************************************************************************************
    // READ: Get all tasks claimed by a specific user for their To-Do Dashboard
    public async Task<EventTaskResult<IEnumerable<EventTask>>> GetTasksClaimedByUserAsync(string userId)
    {
        // Extract items where the user is actively locked into an 'Assigned' or 'SignedUp' tracking status
        var response = await _eventTaskRepository.GetAllAsync
            (
                selector: t => t,
                where: t => t.IsActive && t.Assignments.Any(a =>
                    a.UserId == userId &&
                    (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.SignedUp)),
                sortBy: t => t.CreatedAt,
                includes: [x => x.Event, x => x.Assignments]
            );

        if (!response.Succeeded || response.Result == null)
            return new EventTaskResult<IEnumerable<EventTask>>
            { Succeeded = false, StatusCode = response.StatusCode, ErrorMessage = response.ErrorMessage ?? "Could not load claimed tasks." };

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
    // UPDATE
    public async Task<EventTaskResult> UpdateEventTaskAsync(string userId, string eventId, string eventTaskId, EditTaskFormData formData)
    {
        if (formData == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        var fetchResponse = await _eventTaskRepository.GetEntityAsync
          (
            t => t.Id == eventTaskId && t.EventId == eventId,
            includeChains:
            [q => q.Include(t => t.Event).ThenInclude(e => e.Roles)]
          );

        var eventTaskEntity = fetchResponse.Result;
        if (!fetchResponse.Succeeded || eventTaskEntity == null)
            return new EventTaskResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event task not found." };

        // Check if user is owner or co-owner
        bool isOwnerOrCoOwner = eventTaskEntity.Event.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventTaskResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to edit this event task." };

        formData.MapOnto(eventTaskEntity);
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
        var fetchResponse = await _eventTaskRepository.GetEntityAsync
          (
            t => t.Id == eventTaskId && t.EventId == eventId,
            includeChains:
            [q => q.Include(t => t.Event).ThenInclude(e => e.Roles)]
          );

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
}
