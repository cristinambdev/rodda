using Business.Services;
using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;
using System.Security.Claims;


namespace Presentation.Controllers;


[Authorize]

public class EventsController( IEventService eventService, IEventItemService eventItemService, IEventTaskService eventTaskService, IEventAccessService eventAccessService) : Controller
{
    private readonly IEventService _eventService = eventService;
    private readonly IEventItemService _eventItemService = eventItemService;
    private readonly IEventTaskService _eventTaskService = eventTaskService;
    private readonly IEventAccessService _eventAccessService = eventAccessService;

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events")]
    public async Task<IActionResult> Events()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var response = await _eventService.GetEventsForUserAsync(userId);
        var model = new EventsViewModel
        {
            Events = response.Result?.ToList() ?? [],
            UserId = userId
        };

        ViewData["ActiveNav"] = "events-all";
        return View(model);
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddEventViewModel model)
    {
        if (!ModelState.IsValid)
            return View("CreateNewEvent", model);

        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var addEventFormData = model.MapTo<AddEventFormData>();
        var result = await _eventService.CreateEventAsync(userId, addEventFormData);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View("CreateNewEvent", model);
        }

        return RedirectToAction("Events");
    }

    // **************************************************************************************************************************
    [HttpGet("/events/create")]
    [HttpGet("/Events/CreateNewEvent")]
    public IActionResult CreateNewEvent()
    {
        return View(new AddEventViewModel());
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/update-settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, [FromBody] EditEventViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _eventService.UpdateEventSettingsAsync(userId, id, model.AllowGuestBringItems, model.AllowGuestTasks);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        return Json(new { succeeded = true });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var deleteResult = await _eventService.DeleteEventAsync(userId, id);

        if (!deleteResult.Succeeded)
        {
            if (deleteResult.StatusCode == 404)
                return NotFound();
            return StatusCode(deleteResult.StatusCode, deleteResult.ErrorMessage);
        }

        return RedirectToAction(nameof(Events));
    }

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events/{id}/share-link")]
    public async Task<IActionResult> GetShareLink(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventAccessService.GetOrCreateShareLinkAsync(userId, id);
        if (!result.Succeeded || string.IsNullOrEmpty(result.Result))
        {
            if (result.StatusCode == 404)
                return NotFound();
            return StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        var shareUrl = $"{Request.Scheme}://{Request.Host}/events/{id}?invite={result.Result}";
        return Json(new { url = shareUrl, token = result.Result });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/share-link/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeShareLink(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventAccessService.RevokeShareLinkAsync(userId, id);
        if (!result.Succeeded)
        {
            if (result.StatusCode == 404)
                return NotFound();
            if (result.StatusCode == 403)
                return Forbid();
            return StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        return Json(new { succeeded = true });
    }

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events/{id}")]
    public async Task<IActionResult> EventDetails(string id, [FromQuery] string? invite)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        if (!string.IsNullOrWhiteSpace(invite))
            await _eventAccessService.RedeemShareTokenAsync(userId, id, invite);

        var response = await _eventService.GetEventForUserAsync(userId, id);
        if (!response.Succeeded || response.Result == null)
            return NotFound();

        var eventData = response.Result;
        var model = eventData.MapTo<EventDetailsViewModel>();
        model.EventRoles = eventData.Roles;
        model.CanManageItemsTasks = eventData.Roles.Any(r =>

            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));

        var hasAcceptedAttendance = eventData.Attendances.Any(a =>
            a.UserId == userId && a.Status == AttendanceStatus.Accepted);

        model.CanAddItems = model.CanManageItemsTasks ||
            (model.AllowGuestBringItems && hasAcceptedAttendance);
        model.CanAddTasks = model.CanManageItemsTasks ||
            (model.AllowGuestTasks && hasAcceptedAttendance);

        ViewData["CanManageEvent"] = model.CanManageItemsTasks;

        var itemsResponse = await _eventItemService.GetItemsForEventAsync(userId, id);
        if (itemsResponse.Succeeded && itemsResponse.Result != null)
        {
            var itemList = itemsResponse.Result.ToList();
            ViewData["EventItemsData"] = itemList;
            model.Items = itemList.Select(item =>
            {
                var row = item.MapTo<EventItemViewModel>();
                row.CreatedByDisplayName = item.CreatedByUser?.DisplayName;
                row.Assignments = item.Assignments.Select(a => new EventAssignmentSlotViewModel
                {
                    Id = a.Id,
                    AssigneeType = a.AssigneeType,
                    UserId = a.UserId,
                    DisplayName = a.User?.DisplayName,
                    PlaceholderLabel = a.PlaceholderLabel,
                    Status = a.Status
                }).ToList();
                return row;

            }).ToList();
        }

        var tasksResponse = await _eventTaskService.GetTasksForEventAsync(userId, id);
        if (tasksResponse.Succeeded && tasksResponse.Result != null)
        {
            var taskList = tasksResponse.Result.ToList();
            ViewData["EventTasksData"] = taskList;
            model.Tasks = taskList.Select(task =>
            {
                var row = task.MapTo<EventTaskViewModel>();
                row.TaskLocation = task.TaskLocationName ?? task.TaskLocation?.Street;
                row.CreatedByDisplayName = task.CreatedByUser?.DisplayName;
                row.Assignments = task.Assignments.Select(a => new EventAssignmentSlotViewModel
                {
                    Id = a.Id,
                    AssigneeType = a.AssigneeType,
                    UserId = a.UserId,
                    DisplayName = a.User?.DisplayName,
                    PlaceholderLabel = a.PlaceholderLabel,
                    Status = a.Status
                }).ToList();
                return row;
            }).ToList();
        }

        if (!string.IsNullOrWhiteSpace(invite))
            return RedirectToAction(nameof(EventDetails), new { id });

        return View(model);
    }

    // **************************************************************************************************************************
    // ----------------- CHAT ---------------------
    [HttpPost]
    [Route("/events/{id}/chat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChatMessage(string id, AddChatMessageViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Body))
            return RedirectToAction(nameof(EventDetails), new { id });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var formData = model.MapTo<AddChatMessageFormData>();
        var result = await _eventService.AddChatMessageAsync(id, userId, formData);

        if (!result.Succeeded && result.StatusCode == 404)
            return NotFound();

        return RedirectToAction(nameof(EventDetails), new { id });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/chat/{messageId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChatMessage(string id, string messageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _eventService.DeleteChatMessageAsync(messageId, userId);
        if (!result.Succeeded)
        {
            if (result.StatusCode == 404) return NotFound();
            if (result.StatusCode == 403) return Forbid();
            return StatusCode(result.StatusCode, result.ErrorMessage);
        }
        return RedirectToAction(nameof(EventDetails), new { id });
    }
}


