using Business.Services;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Models;

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
            TempData["OpenModal"] = "tasks";
            return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
        }

        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        model.EventId = eventId;
        var formData = model.MapTo<AddTaskFormData>();

        var result = await _eventTaskService.CreateEventTaskAsync(userId, formData);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Task added successfully!";

        TempData["OpenModal"] = "tasks";
        return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTask(string eventId, string taskId, EditTaskViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid form data submitted.";
            TempData["OpenModal"] = "tasks";
            return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
        }

        var userId = User.GetUserId();
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

        TempData["OpenModal"] = "tasks";
        return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(string eventId, string taskId)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _eventTaskService.DeleteEventTaskAsync(userId, eventId, taskId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Task removed.";

        TempData["OpenModal"] = "tasks";
        return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/claim")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClaimTask(string eventId, string taskId, string? assignmentId = null)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _eventTaskService.ClaimTaskAsync(userId, eventId, taskId, assignmentId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
        {
            var response = await _eventTaskService.GetEventTaskAsync(eventId, taskId);
            var taskName = string.IsNullOrWhiteSpace(response.Result?.Title)
                ? "this task"
                : response.Result!.Title;
            TempData["SuccessMessage"] = $"Thanks for claiming {taskName}";
        }

        TempData["OpenModal"] = "tasks";
        return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
    }

    // **************************************************************************************************************************
    [HttpPost("{taskId}/unclaim")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnclaimTask(string eventId, string taskId, string? assignmentId = null)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _eventTaskService.UnclaimTaskAsync(userId, eventId, taskId, assignmentId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
        {
            var response = await _eventTaskService.GetEventTaskAsync(eventId, taskId);
            var taskName = string.IsNullOrWhiteSpace(response.Result?.Title)
                ? "this task"
                : response.Result!.Title;
            TempData["SuccessMessage"] = $"You have unclaimed {taskName}";
        }

        TempData["OpenModal"] = "tasks";
        return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
    }
}
