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
    Task<EventResult<IEnumerable<Event>>> GetEventsForUserAsync(string userId);
    Task<EventResult<Event>> GetEventForUserAsync(string userId, string id);
    Task<EventResult> UpdateEventAsync(string userId, string eventId, UpdateEventFormData formData);
    Task<EventResult> UpdateEventSettingsAsync(string userId, string eventId, bool allowGuestBringItems, bool allowGuestTasks);
    Task<EventResult> DeleteEventAsync(string userId, string id);
    Task<EventResult> AddChatMessageAsync(string eventId, string userId, AddChatMessageFormData formData);
    Task<EventResult> DeleteChatMessageAsync(string messageId, string currentUserId);
    Task<EventResult> JoinEventAsync(string userId, string eventId, int guestCount = 1);
    Task<EventResult> LeaveEventAsync(string userId, string eventId);
}

public class EventService(IEventRepository eventRepository, IEventChatRepository eventChatRepository, IEventAttendanceRepository eventAttendanceRepository, IEventAccessService eventAccessService) : IEventService
{
    private readonly IEventRepository _eventRepository = eventRepository;
    private readonly IEventChatRepository _eventChatRepository = eventChatRepository;
    private readonly IEventAttendanceRepository _eventAttendanceRepository = eventAttendanceRepository;
    private readonly IEventAccessService _eventAccessService = eventAccessService;

    // **************************************************************************************************************************
    // CREATE
    public async Task<EventResult> CreateEventAsync(string userId, AddEventFormData formData)
    {
        if (formData == null)
            return new EventResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Not all required fields are supplied" };

        var eventEntity = formData.MapTo<EventEntity>();

        if (string.IsNullOrEmpty(eventEntity.Id))
            eventEntity.Id = Guid.NewGuid().ToString();

        if (eventEntity.StartAt.Year < 2000)
            eventEntity.StartAt = DateTimeOffset.UtcNow; // Fixes the "hidden" event bug when no date is added

        // Set the Creator ID
        eventEntity.CreatedByUserId = userId;
        eventEntity.Visibility = EventVisibility.Private;

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
            ? new EventResult { Succeeded = true, StatusCode = 201, EventId = eventEntity.Id }
            : new EventResult { Succeeded = false, StatusCode = 500, ErrorMessage = result.ErrorMessage };
    }

    // **************************************************************************************************************************
    // READ: events the user actively belongs to (host/co-host or accepted attendance).
    public async Task<EventResult<IEnumerable<Event>>> GetEventsForUserAsync(string userId)
    {
        var response = await _eventRepository.GetAllAsync
            (
                selector: e => e,
                orderByDescending: true,
                sortBy: e => e.CreatedAt,
                where: e =>
                    e.Roles.Any(r =>
                        r.UserId == userId &&
                        (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner)) ||
                    e.Attendances.Any(a =>
                        a.UserId == userId && a.Status == AttendanceStatus.Accepted),
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
        if (!response.Succeeded || response.Result == null)
            return new EventResult<IEnumerable<Event>> { Succeeded = false, StatusCode = response.StatusCode, ErrorMessage = response.ErrorMessage };

        var events = response.Result.Select(MapEvent).ToList();
        return new EventResult<IEnumerable<Event>> { Succeeded = true, StatusCode = 200, Result = events };
    }

    // **************************************************************************************************************************
    // READ: single event when the caller has active membership; otherwise 404.
    public async Task<EventResult<Event>> GetEventForUserAsync(string userId, string id)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, id);
        if (!access.Succeeded)
            return new EventResult<Event> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var response = await _eventRepository.GetEntityAsync
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

        if (!response.Succeeded || response.Result == null)
            return new EventResult<Event> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var entity = response.Result;

        if (!_eventAccessService.HasViewAccess(entity, userId))
            return new EventResult<Event> { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var result = MapEvent(entity);

        return new EventResult<Event> { Succeeded = true, StatusCode = 200, Result = result };
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
    // UPDATE: guest item/task policy toggles from event details modals
    // Generated with help of AI
    public async Task<EventResult> UpdateEventSettingsAsync(string userId, string eventId, bool allowGuestBringItems, bool allowGuestTasks)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var fetchResponse = await _eventRepository.GetEntityAsync(
            e => e.Id == eventId,
            includes: [x => x.Roles]);

        var eventEntity = fetchResponse.Result;
        if (!fetchResponse.Succeeded || eventEntity == null)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var isOwnerOrCoOwner = eventEntity.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isOwnerOrCoOwner)
            return new EventResult { Succeeded = false, StatusCode = 403, ErrorMessage = "You do not have permission to edit this event." };

        eventEntity.AllowGuestBringItems = allowGuestBringItems;
        eventEntity.AllowGuestTasks = allowGuestTasks;
        eventEntity.UpdatedAt = DateTime.UtcNow;

        var updateResponse = await _eventRepository.UpdateAsync(eventEntity);
        return updateResponse.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 200 }
            : new EventResult { Succeeded = false, StatusCode = updateResponse.StatusCode, ErrorMessage = updateResponse.ErrorMessage };
    }

    // **************************************************************************************************************************
    // JOIN EVENT: Updates party size for members who already have view access (use share link to enter first).
    public async Task<EventResult> JoinEventAsync(string userId, string eventId, int guestCount = 1)
    {
        if (guestCount < 1)
            return new EventResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Guest count must be at least 1." };

        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var attendanceResponse = await _eventAttendanceRepository.GetEntityAsync(
            a => a.UserId == userId && a.EventId == eventId);

        if (!attendanceResponse.Succeeded || attendanceResponse.Result == null)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var attendance = attendanceResponse.Result;
        if (attendance.Status != AttendanceStatus.Accepted)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        attendance.GuestCount = guestCount;
        attendance.RespondedAt = DateTime.UtcNow;

        var updateResult = await _eventAttendanceRepository.UpdateAsync(attendance);
        return updateResult.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 200 }
            : new EventResult { Succeeded = false, StatusCode = updateResult.StatusCode, ErrorMessage = updateResult.ErrorMessage };
    }

    // **************************************************************************************************************************
    // LEAVE EVENT: Marks the user's attendance as declined (keeps the row for history / re-join).
    public async Task<EventResult> LeaveEventAsync(string userId, string eventId)
    {
        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var response = await _eventAttendanceRepository.GetEntityAsync(
            a => a.UserId == userId && a.EventId == eventId);

        if (!response.Succeeded || response.Result == null)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Attendance record not found." };

        var attendance = response.Result;
        attendance.Status = AttendanceStatus.Declined;
        attendance.RespondedAt = DateTime.UtcNow;

        var updateResult = await _eventAttendanceRepository.UpdateAsync(attendance);
        return updateResult.Succeeded
            ? new EventResult { Succeeded = true, StatusCode = 200 }
            : new EventResult { Succeeded = false, StatusCode = updateResult.StatusCode, ErrorMessage = updateResult.ErrorMessage };
    }

    // **************************************************************************************************************************
    // DELETE
    public async Task<EventResult> DeleteEventAsync(string userId, string id)
    {
        var fetchResponse = await _eventRepository.GetEntityAsync(
            e => e.Id == id,
            includes: [x => x.Roles]);

        if (!fetchResponse.Succeeded || fetchResponse.Result == null)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

        var isHost = fetchResponse.Result.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        if (!isHost)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

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

        var access = await _eventAccessService.VerifyViewAccessAsync(userId, eventId);
        if (!access.Succeeded)
            return new EventResult { Succeeded = false, StatusCode = 404, ErrorMessage = "Event not found." };

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

    // **************************************************************************************************************************
    private static Event MapEvent(EventEntity entity)
    {
        var result = entity.MapTo<Event>();
        result.CoverImageUrl = entity.CoverImageUrl;
        result.CreatedByUserId = entity.CreatedByUserId;
        result.CreatorDisplayName = entity.CreatedByUser?.DisplayName;
        result.Location = new Address
        {
            Name = entity.LocationName,
            Street = entity.LocationStreet,
            Postcode = entity.LocationPostcode,
            City = entity.LocationCity,
            Country = entity.LocationCountry,
        };
        result.Roles = entity.Roles.Select(r => r.MapTo<EventRole>()).ToList();
        result.Attendances = entity.Attendances.Select(a => a.MapTo<EventAttendance>()).ToList();
        result.AllowGuestBringItems = entity.AllowGuestBringItems == true;
        result.AllowGuestTasks = entity.AllowGuestTasks == true;
        result.ItemsTasksEnabled = entity.ItemsTasksEnabled == true;
        result.Visibility = entity.Visibility;
        return result;
    }

}
