using Business.Services;
using Domain.Enums;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Helpers;
using Presentation.Models;
using System.Globalization;
using System.Text.Json;


namespace Presentation.Controllers;


[Authorize]

public class EventsController( IEventService eventService, IEventItemService eventItemService, IEventTaskService eventTaskService, IEventAccessService eventAccessService, IWebHostEnvironment webHostEnvironment) : Controller
{
    private readonly IEventService _eventService = eventService;
    private readonly IEventItemService _eventItemService = eventItemService;
    private readonly IEventTaskService _eventTaskService = eventTaskService;
    private readonly IEventAccessService _eventAccessService = eventAccessService;
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events")]
    public async Task<IActionResult> Events()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var response = await _eventService.GetEventsForUserAsync(userId);
        var events = response.Result?.ToList() ?? [];
        var model = new EventsViewModel
        {
            Events = events,
            UserId = userId,
            CardBadges = await BuildCardBadgesAsync(userId, events),
        };

        ViewData["ActiveNav"] = "events-all";
        ViewData["EventCardBadges"] = model.CardBadges;
        return View(model);
    }

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events/my-events")]
    public async Task<IActionResult> MyEvents()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var response = await _eventService.GetManagedEventsForUserAsync(userId);
        var events = response.Result?.ToList() ?? [];

        ViewData["ActiveNav"] = "events-mine";
        ViewData["PortalEventsJson"] = JsonSerializer.Serialize(PortalEventsHelper.ToPortalPayload(events, userId));

        return View(new EventsViewModel
        {
            Events = events,
            UserId = userId,
        });
    }

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/events/card/{eventId}")]
    public async Task<IActionResult> GetSingleEventCard(string eventId, [FromQuery] bool fromMyEvents = false)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var response = await _eventService.GetEventForUserAsync(userId, eventId);
        if (!response.Succeeded || response.Result == null)
            return NotFound();

        var ev = response.Result;
        ViewData["UserId"] = userId;
        if (fromMyEvents)
            ViewData["DetailFromMyEvents"] = true;

        var badges = await BuildCardBadgesAsync(userId, [ev]);
        ViewData["EventCardBadges"] = badges;

        return PartialView("~/Views/Shared/Partials/EventCardsPartials/_HorizontalEventCard.cshtml", ev);
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddEventViewModel model, IFormFile? cover)
    {
        ApplyCreateFormFields(model, Request.Form);

        ModelState.Remove(nameof(model.JoinButton));
        ModelState.Remove(nameof(model.ChatEnabled));

        if (cover is { Length: > 0 })
        {
            var coverUrl = await ImageUploadHelper.UploadImageAsync(cover, "events", _webHostEnvironment);
            if (coverUrl != null)
                model.CoverImageUrl = coverUrl;
        }

        if (!ModelState.IsValid)
            return View("CreateNewEvent", model);

        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _eventService.CreateEventAsync(userId, model.ToAddFormData());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View("CreateNewEvent", model);
        }

        if (!string.IsNullOrEmpty(result.EventId))
        {
            await PersistDraftItemsAndTasksOnCreateAsync(
                userId,
                result.EventId,
                model.ItemsTasksEnabled == true,
                model.DraftBringItemsJson,
                model.DraftGuestTasksJson);
        }

        return RedirectToAction("Events");
    }

    // **************************************************************************************************************************
    [HttpGet("/events/create")]
    [HttpGet("/Events/CreateNewEvent")]
    public async Task<IActionResult> CreateNewEvent([FromQuery] string? edit)
    {
        if (string.IsNullOrWhiteSpace(edit))
            return View(new AddEventViewModel());

        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var response = await _eventService.GetEventForUserAsync(userId, edit.Trim());
        if (!response.Succeeded || response.Result == null)
            return NotFound();

        var ev = response.Result;
        var canManage = ev.Roles.Any(r =>
            r.UserId == userId &&
            (r.Role == EventRoleType.Owner || r.Role == EventRoleType.CoOwner));
        if (!canManage)
            return Forbid();

        ViewData["EventDetailsModalModel"] = await BuildEventDetailsModalAsync(userId, ev);
        return View(CreateEventFormHelper.FromEvent(ev));
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, AddEventViewModel model, IFormFile? cover)
    {
        ApplyCreateFormFields(model, Request.Form);

        ModelState.Remove(nameof(model.JoinButton));
        ModelState.Remove(nameof(model.ChatEnabled));

        if (cover is { Length: > 0 })
        {
            var coverUrl = await ImageUploadHelper.UploadImageAsync(cover, "events", _webHostEnvironment);
            if (coverUrl != null)
                model.CoverImageUrl = coverUrl;
        }

        if (!ModelState.IsValid)
            return View("CreateNewEvent", model);

        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        model.EventId = id;
        var result = await _eventService.UpdateEventAsync(userId, id, model.ToUpdateFormData());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not save the event.");
            return View("CreateNewEvent", model);
        }

        return RedirectToAction(nameof(EventDetails), new { id = EventUrls.DetailsSegment(result.EventSlug, result.EventId!) });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/update-settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, [FromBody] EditEventViewModel model)
    {
        var userId = User.GetUserId();
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
    [Route("/events/{id}/join")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(string id, int groupSize = 1)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var eventResponse = await _eventService.GetEventForUserAsync(userId, id);
        if (!eventResponse.Succeeded || eventResponse.Result == null)
            return NotFound();

        if (eventResponse.Result.JoinMode == JoinMode.Disabled)
        {
            TempData["ErrorMessage"] = "Joining is not enabled for this event.";
            return RedirectToAction(nameof(EventDetails), new { id });
        }

        var result = await _eventService.JoinEventAsync(userId, id, groupSize);
        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not join the event.";

        return RedirectToAction(nameof(EventDetails), new { id });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/leave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(string id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _eventService.LeaveEventAsync(userId, id);
        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not leave the event.";

        return RedirectToAction(nameof(EventDetails), new { id });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/remove-from-list")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromMyList(string id, string? returnUrl = null)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var result = await _eventService.RemoveFromMyListAsync(userId, id);
        if (!result.Succeeded)
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not remove the event from your list.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Events));
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = User.GetUserId();
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
    // Share link methods created by AI
    [HttpGet]
    [Route("/events/{id}/share-link")]
    public async Task<IActionResult> GetShareLink(string id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var eventId = await _eventService.ResolveEventIdAsync(id);
        if (eventId == null)
            return NotFound();

        var result = await _eventAccessService.GetOrCreateShareLinkAsync(userId, eventId);
        if (!result.Succeeded || string.IsNullOrEmpty(result.Result))
        {
            if (result.StatusCode == 404)
                return NotFound();
            return StatusCode(result.StatusCode, new { error = result.ErrorMessage });
        }

        var eventResponse = await _eventService.GetEventForUserAsync(userId, eventId);
        var pathSegment = eventResponse.Result != null
            ? EventUrls.DetailsSegment(eventResponse.Result.Slug, eventId)
            : eventId;
        var shareUrl = $"{Request.Scheme}://{Request.Host}/events/{pathSegment}?invite={result.Result}";
        return Json(new { url = shareUrl, token = result.Result });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/share-link/revoke")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeShareLink(string id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var eventId = await _eventService.ResolveEventIdAsync(id);
        if (eventId == null)
            return NotFound();

        var result = await _eventAccessService.RevokeShareLinkAsync(userId, eventId);
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
    public async Task<IActionResult> EventDetails(string id, [FromQuery] string? invite, [FromQuery] string? from)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var eventId = await _eventService.ResolveEventIdAsync(id);
        if (eventId == null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(invite))
            await _eventAccessService.RedeemShareTokenAsync(userId, eventId, invite);

        var response = await _eventService.GetEventForUserAsync(userId, eventId);
        if (!response.Succeeded || response.Result == null)
            return NotFound();

        var eventData = response.Result;
        if (!string.IsNullOrWhiteSpace(eventData.Slug)
            && !string.Equals(id, eventData.Slug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(EventDetails), new { id = eventData.Slug, invite, from });
        }

        var model = eventData.MapTo<EventDetailsViewModel>();
        model.CreatorDisplayLabel = string.Equals(eventData.CreatedByUserId, userId, StringComparison.Ordinal)
            ? "Myself"
            : (string.IsNullOrWhiteSpace(eventData.CreatorDisplayName) ? "Organizer" : eventData.CreatorDisplayName);
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

        var myAttendance = eventData.Attendances.FirstOrDefault(a => a.UserId == userId);
        model.UserHasJoined = myAttendance?.Status == AttendanceStatus.Accepted;
        model.UserGuestCount = Math.Max(1, myAttendance?.GuestCount ?? 1);
        model.TotalJoinedGuests = eventData.Attendances
            .Where(a => a.Status == AttendanceStatus.Accepted)
            .Sum(a => Math.Max(1, a.GuestCount));
        model.Attendees = eventData.Attendances
            .Select(a => new EventAttendeeViewModel
            {
                Id = a.Id,
                EventId = a.EventId,
                UserId = a.UserId,
                DisplayName = ResolveAssignmentDisplayName(a.User),
                GuestCount = Math.Max(1, a.GuestCount),
                Status = a.Status,
                RespondedAt = a.RespondedAt,
            })
            .ToList();

        if (model.CanManageItemsTasks)
            model.HostGuestRoster = BuildHostGuestRoster(eventData);

        model.ChatMessages = eventData.ChatMessages
            .Select(m =>
            {
                var row = m.MapTo<ChatMessageViewModel>();
                row.IsFromCurrentUser = string.Equals(m.AuthorUserId, userId, StringComparison.Ordinal);
                row.AuthorDisplay = row.IsFromCurrentUser
                    ? "You"
                    : (!string.IsNullOrWhiteSpace(m.AuthorDisplay)
                        ? m.AuthorDisplay
                        : (m.Author?.DisplayName ?? "Guest"));
                row.CanDelete = row.IsFromCurrentUser || model.CanManageItemsTasks;
                return row;
            })
            .ToList();

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
                    DisplayName = ResolveAssignmentDisplayName(a.User),
                    PlaceholderLabel = a.PlaceholderLabel,
                    Status = a.Status
                }).ToList();
                return row;

            }).ToList();
        }

        model.CanClaimItems = model.CanManageItemsTasks ||
            (hasAcceptedAttendance && model.Items.Count > 0);

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
                    DisplayName = ResolveAssignmentDisplayName(a.User),
                    PlaceholderLabel = a.PlaceholderLabel,
                    Status = a.Status
                }).ToList();
                return row;
            }).ToList();
        }

        model.CanClaimTasks = model.CanManageItemsTasks ||
            (hasAcceptedAttendance && model.Tasks.Count > 0);

        if (!string.IsNullOrWhiteSpace(invite))
            return RedirectToAction(nameof(EventDetails), new { id });

        ViewData["ActiveNav"] = string.Equals(from, "myevents", StringComparison.OrdinalIgnoreCase)
            ? "events-mine"
            : "events-all";

        return View(model);
    }

    // **************************************************************************************************************************
    // ----------------- CHAT ---------------------
    // **************************************************************************************************************************
    [HttpPost]
    [Route("/events/{id}/chat")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddChatMessage(string id, AddChatMessageViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Body))
            return RedirectToAction(nameof(EventDetails), new { id });

        var userId = User.GetUserId();
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
        var userId = User.GetUserId();
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

    // **************************************************************************************************************************
    // ----------------- EVENT DETAILS MODALS ---------------------
    // **************************************************************************************************************************
    private async Task AttachEventDetailsModalForEditAsync(string userId, string eventId)
    {
        var response = await _eventService.GetEventForUserAsync(userId, eventId);
        if (response.Succeeded && response.Result != null)
            ViewData["EventDetailsModalModel"] = await BuildEventDetailsModalAsync(userId, response.Result);
    }

    // **************************************************************************************************************************
    private async Task<EventDetailsViewModel> BuildEventDetailsModalAsync(string userId, Event eventData)
    {
        var model = EventFormModalHelper.CreateShell(eventData, userId);

        var itemsResponse = await _eventItemService.GetItemsForEventAsync(userId, eventData.Id);
        if (itemsResponse.Succeeded && itemsResponse.Result != null)
            EventFormModalHelper.ApplyItems(model, itemsResponse.Result, ResolveAssignmentDisplayName);

        var tasksResponse = await _eventTaskService.GetTasksForEventAsync(userId, eventData.Id);
        if (tasksResponse.Succeeded && tasksResponse.Result != null)
            EventFormModalHelper.ApplyTasks(model, tasksResponse.Result, ResolveAssignmentDisplayName);

        return model;
    }

    // **************************************************************************************************************************
    // Generated by AI
    private static void ApplyCreateFormFields(AddEventViewModel model, IFormCollection form)
    {
        if (!string.IsNullOrWhiteSpace(model.Date))
        {
            var timePart = string.IsNullOrWhiteSpace(model.Time) ? "00:00" : model.Time.Trim();
            if (DateTime.TryParse(
                    $"{model.Date.Trim()} {timePart}",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal,
                    out var localStart))
            {
                model.StartAt = new DateTimeOffset(localStart);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Location))
            model.LocationName = model.Location.Trim();

        if (string.IsNullOrWhiteSpace(model.Timezone))
            model.Timezone = "Europe/Stockholm";

        model.JoinButton = IsFormCheckboxChecked(form, nameof(model.JoinButton), "joinButton");
        model.ChatEnabled = IsFormCheckboxChecked(form, nameof(model.ChatEnabled), "chatEnabled");
        model.JoinMode = model.JoinButton ? JoinMode.Open : JoinMode.Disabled;

        NormalizePaymentFields(model);
    }

    // **************************************************************************************************************************
    private static bool IsFormCheckboxChecked(IFormCollection form, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!form.TryGetValue(key, out var values))
                continue;

            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                    continue;

                if (value == "1"
                    || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    // **************************************************************************************************************************
    private async Task PersistDraftItemsAndTasksOnCreateAsync(
        string userId,
        string eventId,
        bool itemsTasksEnabled,
        string? bringItemsJson,
        string? guestTasksJson)
    {
        if (!itemsTasksEnabled)
            return;

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        if (!string.IsNullOrWhiteSpace(bringItemsJson))
        {
            var items = JsonSerializer.Deserialize<List<DraftBringItemPayload>>(bringItemsJson, jsonOptions);
            if (items != null)
            {
                var sort = 0;
                foreach (var item in items)
                {
                    var title = item.Title?.Trim();
                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    var people = item.People < 1 ? 1 : Math.Min(item.People, 99);
                    await _eventItemService.CreateEventItemAsync(userId, new AddItemFormData
                    {
                        EventId = eventId,
                        Title = title,
                        Amount = item.Amount.TrimOrNull(),
                        PeopleNeeded = people,
                        SortOrder = sort++,
                    });
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(guestTasksJson))
        {
            var tasks = JsonSerializer.Deserialize<List<DraftGuestTaskPayload>>(guestTasksJson, jsonOptions);
            if (tasks != null)
            {
                var sort = 0;
                foreach (var task in tasks)
                {
                    var title = task.Title?.Trim();
                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    var people = task.People < 1 ? 1 : Math.Min(task.People, 99);
                    await _eventTaskService.CreateEventTaskAsync(userId, new AddTaskFormData
                    {
                        EventId = eventId,
                        Title = title,
                        TaskTime = task.Time.TrimOrNull(),
                        TaskLocation = task.Location.TrimOrNull(),
                        PeopleNeeded = people,
                        SortOrder = sort++,
                    });
                }
            }
        }
    }

    // **************************************************************************************************************************
    private static void NormalizePaymentFields(AddEventViewModel model)
    {
        model.PaymentMethod = model.PaymentMethod.TrimOrNull();
        model.PaymentNumber = model.PaymentNumber.TrimOrNull();
        model.PaymentName = model.PaymentName.TrimOrNull();
        model.PaymentComment = model.PaymentComment.TrimOrNull();
        model.PaymentAmount = model.PaymentAmount.TrimOrNull();
    }

    // **************************************************************************************************************************
    private static List<EventGuestRosterViewModel> BuildHostGuestRoster(Event eventData)
    {
        var byUser = new Dictionary<string, EventGuestRosterViewModel>(StringComparer.Ordinal);

        foreach (var role in eventData.Roles)
        {
            var roleLabel = role.Role == EventRoleType.Owner ? "Owner" : "Co-owner";
            byUser[role.UserId] = new EventGuestRosterViewModel
            {
                UserId = role.UserId,
                DisplayName = ResolveAssignmentDisplayName(role.User),
                RoleLabel = roleLabel,
                Status = AttendanceStatus.Pending,
                GuestCount = 1,
            };
        }

        foreach (var attendance in eventData.Attendances)
        {
            if (byUser.TryGetValue(attendance.UserId, out var row))
            {
                row.Status = attendance.Status;
                row.GuestCount = Math.Max(1, attendance.GuestCount);
                if (string.IsNullOrWhiteSpace(row.DisplayName))
                    row.DisplayName = ResolveAssignmentDisplayName(attendance.User);
            }
            else
            {
                byUser[attendance.UserId] = new EventGuestRosterViewModel
                {
                    UserId = attendance.UserId,
                    DisplayName = ResolveAssignmentDisplayName(attendance.User),
                    GuestCount = Math.Max(1, attendance.GuestCount),
                    Status = attendance.Status,
                };
            }
        }

        return byUser.Values
            .OrderByDescending(r => string.Equals(r.UserId, eventData.CreatedByUserId, StringComparison.Ordinal))
            .ThenByDescending(r => r.RoleLabel == "Owner")
            .ThenByDescending(r => r.RoleLabel == "Co-owner")
            .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // **************************************************************************************************************************
    private async Task<IReadOnlyDictionary<string, EventCardBadgeHelper.CardBadges>> BuildCardBadgesAsync(string userId, IEnumerable<Event> events)
    {
        var map = new Dictionary<string, EventCardBadgeHelper.CardBadges>(StringComparer.Ordinal);
        foreach (var ev in events)
        {
            if (!ev.ItemsTasksEnabled)
            {
                map[ev.Id] = new EventCardBadgeHelper.CardBadges(null, false);
                continue;
            }

            var itemsResponse = await _eventItemService.GetItemsForEventAsync(userId, ev.Id);
            var tasksResponse = await _eventTaskService.GetTasksForEventAsync(userId, ev.Id);
            var items = itemsResponse.Result ?? [];
            var tasks = tasksResponse.Result ?? [];
            map[ev.Id] = EventCardBadgeHelper.Compute(ev.ItemsTasksEnabled, userId, items, tasks);
        }

        return map;
    }

    // **************************************************************************************************************************
    private static string? ResolveAssignmentDisplayName(User? user)
    {
        if (user == null)
            return null;

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            return user.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email.Trim();

        return null;
    }

}


