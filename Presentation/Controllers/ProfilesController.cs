using Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using Presentation.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Presentation.Controllers
{
    [Authorize]
    public class ProfilesController(IEventService eventService) : Controller
    {
        private readonly IEventService _eventService = eventService;

        // **************************************************************************************************************************
        public IActionResult Index()
        {
            return RedirectToAction("Community", "Community");
        }

        // **************************************************************************************************************************
        public async Task<IActionResult> MyEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            var response = await _eventService.GetEventsForUserAsync(userId);
            var events = response.Result?.ToList() ?? [];

            ViewData["ActiveNav"] = "events-mine";
            ViewData["PortalEventsJson"] = JsonSerializer.Serialize(PortalEventsHelper.ToPortalPayload(events, userId));

            return View(new EventsViewModel
            {
                Events = events,
                UserId = userId
            });
        }
    }
}
