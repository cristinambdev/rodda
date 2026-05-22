using Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Helpers;

namespace Presentation.Controllers;

[Authorize]
public class HomeController(IEventItemService eventItemService, IEventTaskService eventTaskService) : Controller
{
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

        var items = itemsResponse.Succeeded ? itemsResponse.Result ?? [] : [];
        var tasks = tasksResponse.Succeeded ? tasksResponse.Result ?? [] : [];

        ViewData["ActiveNav"] = "home";

        var model = HomeTodosHelper.Build(items, tasks, userId, range);
        return View(model);
    }
}
