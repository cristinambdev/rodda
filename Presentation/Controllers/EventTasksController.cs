using Business.Services;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;
using System.Security.Claims;

namespace Presentation.Controllers;

[Authorize]
[Route("events/{eventId}/tasks")]
public class EventTasksController(IEventTaskService eventTaskService) : Controller
{
    private readonly IEventTaskService _eventTaskService = eventTaskService;

    // **************************************************************************************************************************
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask(string eventId, AddTaskViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid form data submitted.";
            return RedirectToEventDetails(eventId);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        model.EventId = eventId;
        var formData = model.MapTo<AddTaskFormData>();

        var result = await _eventTaskService.CreateEventTaskAsync(userId, formData);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Task added successfully!";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTask(string eventId, string taskId, EditTaskViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid form data submitted.";
            return RedirectToEventDetails(eventId);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        model.EventId = eventId;
        model.TaskId = taskId;
        var formData = model.MapTo<EditTaskFormData>();

        var result = await _eventTaskService.UpdateEventTaskAsync(userId, eventId, taskId, formData);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Task updated successfully!";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(string eventId, string taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventTaskService.DeleteEventTaskAsync(userId, eventId, taskId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Task removed.";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/claim")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClaimTask(string eventId, string taskId, string? assignmentId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventTaskService.ClaimTaskAsync(userId, eventId, taskId, assignmentId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Thanks for volunteering!";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/unclaim")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnclaimTask(string eventId, string taskId, string? assignmentId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventTaskService.UnclaimTaskAsync(userId, eventId, taskId, assignmentId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "You have backed out of this task.";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    private IActionResult RedirectToEventDetails(string eventId)
    {
        return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
    }
}
