using Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Helpers;
using System.Text.Json;

namespace Presentation.Controllers;

[Authorize]
public class HomeController(
    IEventService eventService,
    IEventItemService eventItemService,
    IEventTaskService eventTaskService) : Controller
{
    private readonly IEventService _eventService = eventService;
    private readonly IEventItemService _eventItemService = eventItemService;
    private readonly IEventTaskService _eventTaskService = eventTaskService;

    // **************************************************************************************************************************
    [Route("/index")]
    public async Task<IActionResult> Index([FromQuery] string? range)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var itemsResponse = await _eventItemService.GetItemsForHomeTodosAsync(userId);
        var tasksResponse = await _eventTaskService.GetTasksForHomeTodosAsync(userId);
        var eventsResponse = await _eventService.GetEventsForUserAsync(userId);

        var items = itemsResponse.Succeeded ? itemsResponse.Result ?? [] : [];
        var tasks = tasksResponse.Succeeded ? tasksResponse.Result ?? [] : [];
        var events = eventsResponse.Succeeded ? eventsResponse.Result ?? [] : [];

        ViewData["ActiveNav"] = "home";
        ViewData["PortalEventsJson"] = JsonSerializer.Serialize(PortalEventsHelper.ToPortalPayload(events, userId));

        var model = HomeTodosHelper.Build(items, tasks, userId, range);
        return View(model);
    }
}
