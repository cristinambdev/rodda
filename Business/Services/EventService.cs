using Business.Dtos;
using Data.Entities;
using Data.Repositories;
using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Business.Services;

// Handles EventEntity, EventRoleEntity, EventAttendanceEntity
public interface IEventService
{
    Task<EventResult> CreateEventAsync(string userId, AddEventFormData formData);
    Task<EventResult<IEnumerable<Event>>> GetEventsAsync();
    Task<EventResult<Event>> GetEventAsync(string id);
    Task<EventResult> UpdateEventAsync(string userId, string eventId, UpdateEventFormData formData);
    Task<EventResult> DeleteEventAsync(string id);
    Task<EventResult> AddChatMessageAsync(string eventId, string userId, AddChatMessageFormData formData);
    Task<EventResult> DeleteChatMessageAsync(string messageId, string currentUserId);
}

public class EventService(IEventRepository eventRepository, IEventChatRepository eventChatRepository) : IEventService
{
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IEventChatRepository _eventChatRepository = eventChatRepository;

    // **************************************************************************************************************************
    // CREATE
    public async Task<EventResult> CreateEventAsync(string userId, AddEventFormData formData)
    {
        if (formData == null) return new EventResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        var eventEntity = formData.MapTo<EventEntity>();

        // Set the Creator ID
        eventEntity.CreatedByUserId = userId;

        // Add user "Owner"
        eventEntity.Roles.Add(new EventRoleEntity
        {
            UserId = userId,
            Role = EventRoleType.Owner
        });

        // Add user as attendee
        eventEntity.Attendances.Add(new EventAttendanceEntity
        {
            UserId = userId,
            Status = AttendanceStatus.Accepted,
            GuestCount = 1
        });

        var result = await _eventRepository.AddAsync(eventEntity);
        return result.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 201 }
            : new EventResult { Succeeded = false, StatusCode = 500, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // READ
    public async Task<EventResult<IEnumerable<Event>>> GetEventsAsync()
    {
        var response = await _eventRepository.GetAllAsync
            (
                selector: e => e,
                orderByDescending: true,
                sortBy: e => e.CreatedAt,
                where: null,
                includes:
                [
                    x => x.CreatedByUser,
                    x => x.Roles,
                    x => x.Attendances
                ],
                includeChains:
                [
                    q => q.Include(x => x.ChatMessages).ThenInclude(c => c.AuthorUser)
                ]
            );
        return new EventResult<IEnumerable<Event>> { Succeeded = true, StatusCode = 200, Result = response.Result!.MapTo<IEnumerable<Event>>() };
    }

    // **************************************************************************************************************************
    // READ
    public async Task<EventResult<Event>> GetEventAsync(string id)
    {
        var response = await _eventRepository.GetAsync
            (
                where: e => e.Id == id,
                includes:
                [
                    x => x.CreatedByUser,
                    x => x.Roles,
                    x => x.Attendances
                ],
                includeChains:
                [
                    q => q.Include(x => x.ChatMessages).ThenInclude(c => c.AuthorUser)
                ]
            );
        return response.Succeeded
            ? new EventResult<Event> { Succeeded = true, StatusCode = 200, Result = response.Result }
            : new EventResult<Event> { Succeeded = false, StatusCode = 404, ErrorMessage = $"Event with id {id} was not found" };
    }

    // **************************************************************************************************************************
    // Generated with help of AI
    // UPDATE
    public async Task<EventResult> UpdateEventAsync(string userId, string eventId, UpdateEventFormData formData)
    {
        if (formData == null)
            return new EventResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        var fetchResponse = await _eventRepository.GetEntityAsync(e => e.Id == userId);

        var eventEntity = fetchResponse.Result;
        if (!fetchResponse.Succeeded || eventEntity == null)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        // Check if user is owner or co-owner
        var userRole = eventEntity.Roles.FirstOrDefault(r => r.UserId == userId);
        if (userRole == null || (userRole.Role != EventRoleType.Owner && userRole.Role != EventRoleType.CoOwner))
            return new EventResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to edit this event." };

        formData.MapOnto(eventEntity);
        eventEntity.UpdatedAt = DateTime.UtcNow;

        var updateResponse = await _eventRepository.UpdateAsync(eventEntity);
        return updateResponse.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 200 }
            : new EventResult { Succeeded = false, StatusCode = 500, ErrorMessage = updateResponse.ErrorMessage };
    }

    // **************************************************************************************************************************
    // DELETE
    public async Task<EventResult> DeleteEventAsync(string id)
    {
        var fetchResponse = await _eventRepository.GetEntityAsync(e => e.Id == id);
        if (!fetchResponse.Succeeded || fetchResponse.Result == null)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = $"Event with id {id} was not found" };

        // Delete
        var response = await _eventRepository.DeleteAsync(fetchResponse.Result);
        return response.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 200 }
            : new EventResult { Succeeded = false, StatusCode = 500, ErrorMessage = response.ErrorMessage };
    }

    // **************************************************************************************************************************
    // ------------------ CHAT ---------------------------------
    // **************************************************************************************************************************
    // Add Chat message
    public async Task<EventResult> AddChatMessageAsync(string eventId, string userId, AddChatMessageFormData formData)
    {
        if (formData == null)
            return new EventResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Message cannot be empty." };

        var newChatMessage = formData.MapTo<EventChatEntity>();
        var result = await _eventChatRepository.AddAsync(newChatMessage);

        return result.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 200 }
            : new EventResult { Succeeded = false, StatusCode = 500, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // Delete Chat message
    public async Task<EventResult> DeleteChatMessageAsync(string messageId, string currentUserId)
    {
        // Fetch the message AND the Event Roles to check permissions
        var fetchResponse = await _eventChatRepository.GetEntityAsync(
            where: m => m.Id == messageId,
            includeChains:
            [
                q => q.Include(m => m.Event).ThenInclude(e => e.Roles)
            ]
        );

        if (!fetchResponse.Succeeded || fetchResponse.Result == null)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Message not found." };

        var message = fetchResponse.Result;

        //  PERMISSION CHECK
        // Check if they are message author
        bool isAuthor = message.AuthorUserId == currentUserId;

        // Check if they are event owner or co-owner
        bool isEventOwner = message.Event?.Roles.Any(r =>
            r.UserId == currentUserId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner)) ?? false;

        // If they are neither, fail to delete
        if (!isAuthor && !isEventOwner)
            return new EventResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to delete this message." };

        // Delete the message
        var deleteResult = await _eventChatRepository.DeleteAsync(message);

        return deleteResult.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 200 }
            : new EventResult { Succeeded = false, StatusCode = 500, ErrorMessage = deleteResult.ErrorMessage };
    }

}
