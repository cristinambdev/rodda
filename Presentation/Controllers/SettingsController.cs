using Business.Services;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Helpers;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
public class SettingsController( IUserService userService, SignInManager<Data.Entities.UserEntity> signInManager, IWebHostEnvironment webHostEnvironment) : Controller
{
    private readonly IUserService _userService = userService;
    private readonly SignInManager<Data.Entities.UserEntity> _signInManager = signInManager;
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

    // **************************************************************************************************************************
    [HttpGet]
    [Route("/settings")]
    public async Task<IActionResult> Index()
    {
        var userName = User.Identity?.Name;
        var profile = await _userService.GetUserProfileAsync(userName);
        if (profile == null)
            return Unauthorized();

        ViewData["ActiveNav"] = "profile";
        return View(new SettingsViewModel
        {
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            ProfileImageUrl = profile.ProfileImageUrl,
        });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/settings/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        [FromForm] string? displayName,
        [FromForm] string? email,
        IFormFile? avatar)
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        string? profileImageUrl = null;
        var hasAvatarField = Request.Form.ContainsKey("clearAvatar");

        if (avatar is { Length: > 0 })
        {
            if (!avatar.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { succeeded = false, errorMessage = "Please choose an image file." });

            profileImageUrl = await ImageUploadHelper.UploadImageAsync(avatar, "profiles", _webHostEnvironment);
            if (profileImageUrl == null)
                return StatusCode(500, new { succeeded = false, errorMessage = "Could not save profile picture." });
        }
        else if (hasAvatarField && Request.Form["clearAvatar"] == "1")
        {
            profileImageUrl = "";
        }

        var formData = new UpdateUserFormData
        {
            UserName = userName,
            DisplayName = displayName?.Trim(),
            Email = email?.Trim(),
        };

        if (profileImageUrl != null)
            formData.ProfileImageUrl = profileImageUrl;

        var result = await _userService.UpdateUserAsync(formData);
        if (!result.Succeeded)
            return StatusCode(result.StatusCode, new { succeeded = false, errorMessage = result.ErrorMessage });

        var userId = User.GetUserId();
        var identityUser = userId != null
            ? await _signInManager.UserManager.FindByIdAsync(userId)
            : await _signInManager.UserManager.FindByNameAsync(userName);

        if (identityUser != null)
            await _signInManager.RefreshSignInAsync(identityUser);

        var updatedUserName = identityUser?.UserName ?? userName;
        var updated = await _userService.GetUserProfileAsync(updatedUserName);

        return Json(new
        {
            succeeded = true,
            displayName = updated?.DisplayName ?? displayName?.Trim() ?? "",
            email = updated?.Email ?? email?.Trim() ?? "",
            profileImageUrl = updated?.ProfileImageUrl,
        });
    }

    // **************************************************************************************************************************
    [HttpPost]
    [Route("/settings/delete-account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount()
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized();

        var result = await _userService.DeleteUserAsync(userName);
        if (!result.Succeeded)
            return StatusCode(result.StatusCode, new { succeeded = false, errorMessage = result.ErrorMessage });

        await _signInManager.SignOutAsync();

        return Json(new
        {
            succeeded = true,
            redirectUrl = Url.Action(nameof(AuthController.SignIn), "Auth") ?? "/Auth/SignIn",
        });
    }
}
