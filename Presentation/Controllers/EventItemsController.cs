using Business.Services;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;
using System.Security.Claims;

namespace Presentation.Controllers;

[Authorize]
[Route("events/{eventId}/items")]
public class EventItemsController(IEventItemService eventItemService) : Controller
{
    private readonly IEventItemService _eventItemService = eventItemService;

    // **************************************************************************************************************************
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateItem(string eventId, AddItemViewModel model)
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
        var formData = model.MapTo<AddItemFormData>();

        var result = await _eventItemService.CreateEventItemAsync(userId, formData);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Item added successfully!";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{itemId}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditItem(string eventId, string itemId, EditItemViewModel model)
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
        model.ItemId = itemId;
        var formData = model.MapTo<EditItemFormData>();

        var result = await _eventItemService.UpdateEventItemAsync(userId, eventId, itemId, formData);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Item updated successfully!";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{itemId}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(string eventId, string itemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventItemService.DeleteEventItemAsync(userId, eventId, itemId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Item removed.";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{itemId}/claim")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClaimItem(string eventId, string itemId, string? assignmentId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventItemService.ClaimItemAsync(userId, eventId, itemId, assignmentId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "Thanks for volunteering!";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    [HttpPost("{itemId}/unclaim")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnclaimItem(string eventId, string itemId, string? assignmentId = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _eventItemService.UnclaimItemAsync(userId, eventId, itemId, assignmentId);

        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage;
        else
            TempData["SuccessMessage"] = "You have backed out of this item.";

        return RedirectToEventDetails(eventId);
    }

    // **************************************************************************************************************************
    private IActionResult RedirectToEventDetails(string eventId)
    {
        return RedirectToAction(nameof(EventsController.EventDetails), "Events", new { id = eventId });
    }
}
