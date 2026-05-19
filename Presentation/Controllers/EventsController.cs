using Business.Services;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;
using System.Security.Claims;

namespace Presentation.Controllers;

[Authorize]
public class EventsController(IEventService eventService) : Controller
{
    private readonly IEventService _eventService = eventService;

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events")]
    public async Task<IActionResult> Events()
    {
        var model = new EventsViewModel
        {
            Events = await _eventService.GetEventsAsync()
        };
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
            return View("Create", model);
        }

        return RedirectToAction("Events");
    }

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events/create")]
    public IActionResult CreateNewEvent()
    {
        return View(new AddEventViewModel());
    }

    // **************************************************************************************************************************
    [HttpPost]
    public IActionResult Update(EditEventViewModel model)
    {
        return Json(new { });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var deleteResult = await _eventService.DeleteEventAsync(id);

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
    [Route("/events/{id}")]
    public async Task<IActionResult> EventDetails(string id)
    {
        var response = await _eventService.GetEventAsync(id);

        if (!response.Succeeded || response.Result == null)
            return NotFound();

        var model = response.Result.MapTo<EventDetailsViewModel>();

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
        await _eventService.AddChatMessageAsync(id, userId, formData);

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
