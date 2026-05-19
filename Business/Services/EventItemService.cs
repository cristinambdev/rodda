using Business.Dtos;
using Data.Entities;
using Data.Repositories;
using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

public interface IEventItemService
{
    Task<EventItemResult> CreateEventItemAsync(string userId, AddItemFormData formData);
    Task<EventItemResult> DeleteEventItemAsync(string userId, string eventId, string eventItemId);
    Task<EventItemResult<EventItem>> GetEventItemAsync(string eventId, string eventItemId);
    Task<EventItemResult<IEnumerable<EventItem>>> GetItemsClaimedByUserAsync(string userId);
    Task<EventItemResult<IEnumerable<EventItem>>> GetItemsForEventAsync(string eventId);
    Task<EventItemResult> UpdateEventItemAsync(string userId, string eventId, string eventItemId, EditItemFormData formData);
}

// Handles EventItemEntity, EventItemAssignmentEntity

public class EventItemService(IEventItemRepository eventItemRepository, IEventItemAssignmentRepository eventItemAssignmentRepository, IEventRepository eventRepository) : IEventItemService
{
    private readonly IEventItemRepository _eventItemRepository = eventItemRepository;
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IEventItemAssignmentRepository _eventItemAssignmentRepository = eventItemAssignmentRepository;

    // **************************************************************************************************************************
    // CREATE
    public async Task<EventItemResult> CreateEventItemAsync(string userId, AddItemFormData formData)
    {
        if (formData == null)
            return new EventItemResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        // Fetch the event with roles to check permissions
        var fetchResponse = await _eventRepository.GetEntityAsync
        (
            e => e.Id == formData.EventId,
            includes: [x => x.Roles]
        );
        var fetchEvent = fetchResponse.Result;
        if (!fetchResponse.Succeeded || fetchEvent == null)
            return new EventItemResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Event with id {formData.EventId} was not found" };

        // Permssion check
        bool isOwnerOrCoOwner = fetchEvent.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventItemResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to create an item for this event." };

        var newEventItem = formData.MapTo<EventItemEntity>();

        newEventItem.CreatedByUserId = userId;
        newEventItem.IsActive = true;
        newEventItem.CreatedAt = DateTime.UtcNow;

        // Populate the original values
        newEventItem.OriginalTitle = formData.Title;
        newEventItem.OriginalAmount = formData.Amount;
        newEventItem.OriginalSignupMode = formData.SignupMode;


        var result = await _eventItemRepository.AddAsync(newEventItem);
        return result.Succeeded
            ? new EventItemResult { Succeeded = true, StatusCode = 201 }
            : new EventItemResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };
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
    public async Task<EventItemResult<IEnumerable<EventItem>>> GetItemsForEventAsync(string eventId)
    {
        // Grab only active (non-soft-deleted) items for this event, complete with user assignments
        var response = await _eventItemRepository.GetAllAsync
            (
                selector: i => i,
                where: i => i.EventId == eventId && i.IsActive,
                sortBy: i => i.SortOrder,
                includes: [x => x.Assignments]
            );

        if (!response.Succeeded || response.Result == null)
            return new EventItemResult<IEnumerable<EventItem>>
            { Succeeded = false, StatusCode = response.StatusCode, ErrorMessage = response.ErrorMessage ?? "Could not load event items." };

        return new EventItemResult<IEnumerable<EventItem>>
        { Succeeded = true, StatusCode = 200, Result = response.Result.MapTo<IEnumerable<EventItem>>() };
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
    // UPDATE
    public async Task<EventItemResult> UpdateEventItemAsync(string userId, string eventId, string eventItemId, EditItemFormData formData)
    {
        if (formData == null)
            return new EventItemResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        // Include the event and its roles
        var fetchResponse = await _eventItemRepository.GetEntityAsync(
            e => e.Id == eventItemId && e.EventId == eventId,
            includeChains:
            [
                q => q.Include(i => i.Event)
                .ThenInclude(e => e.Roles)
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

        formData.MapOnto(eventItemEntity);

        var result = await _eventItemRepository.UpdateAsync(eventItemEntity);
        return result.Succeeded
            ? new EventItemResult { Succeeded = true, StatusCode = 200 }
            : new EventItemResult { Succeeded = false, StatusCode = result.StatusCode, ErrorMessage = result.ErrorMessage };

    }

    // **************************************************************************************************************************
    // DELETE
    public async Task<EventItemResult> DeleteEventItemAsync(string userId, string eventId, string eventItemId)
    {
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

}
