using Business.Services;
using Data.Entities;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Presentation.Controllers;

public class AuthController(IAuthService authService, UserManager<UserEntity> userManager, SignInManager<UserEntity> signInManager) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly UserManager<UserEntity> _userManager = userManager;
    private readonly SignInManager<UserEntity> _signInManager = signInManager;

    // **************************************************************************************************************************
    [HttpGet("/auth/sign-in")]
    public IActionResult SignIn(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/events");

        ViewBag.ReturnUrl = returnUrl ?? "/index";
        return View(new SignInViewModel());
    }

    // **************************************************************************************************************************
    [HttpPost("/auth/sign-in")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignIn(SignInViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl ?? "/index";

        if (!ModelState.IsValid)
            return View(model);

        var signInFormData = model.MapTo<SignInFormData>();

        var result = await _authService.SignInAsync(signInFormData);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Sign in failed.");
            return View(model);
        }

        return RedirectToLocal(returnUrl);
    }

    // **************************************************************************************************************************
    [HttpGet("/auth/sign-up")]
    public IActionResult SignUp()
    {
        return View(new SignUpViewModel());
    }

    // **************************************************************************************************************************
    [HttpPost("/auth/sign-up")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(SignUpViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var signUpFormData = model.MapTo<SignUpFormData>();

        var result = await _authService.SignUpAsync(signUpFormData);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Sign up failed.");
            return View(model);
        }

        return Redirect("/index");
    }

    // **************************************************************************************************************************
    [HttpPost("/auth/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        return Redirect("/auth/sign-in");
    }

    // *************************************************************************************************
    // Redirects the user to Google
    [HttpPost("/auth/external-login")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalSignInCallback), "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    // *************************************************************************************************
    [HttpGet("/auth/external-sign-in-callback")]
    public async Task<IActionResult> ExternalSignInCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrEmpty(remoteError))
        {
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            ViewBag.ReturnUrl = returnUrl ?? "/index";
            return View("SignIn", new SignInViewModel());
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectToAction(nameof(SignIn), new { returnUrl });

        var signInResult = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (signInResult.Succeeded)
        {
            await SignInWithProfileClaimsAsync(info);
            return RedirectToLocal(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, "Google did not provide an email address for this account.");
            ViewBag.ReturnUrl = returnUrl ?? "/index";
            return View("SignIn", new SignInViewModel());
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            var displayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "Google User";
            user = new UserEntity { UserName = email, Email = email, DisplayName = displayName };

            var identityResult = await _userManager.CreateAsync(user);
            if (!identityResult.Succeeded)
            {
                foreach (var error in identityResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                ViewBag.ReturnUrl = returnUrl ?? "/index";
                return View("SignIn", new SignInViewModel());
            }
        }

        var linkResult = await _userManager.AddLoginAsync(user, info);
        if (!linkResult.Succeeded)
        {
            foreach (var error in linkResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.ReturnUrl = returnUrl ?? "/index";
            return View("SignIn", new SignInViewModel());
        }

        await SignInWithProfileClaimsAsync(info, user);
        return RedirectToLocal(returnUrl);
    }

    // **************************************************************************************************************************
    private async Task SignInWithProfileClaimsAsync(ExternalLoginInfo info, UserEntity? user = null)
    {
        user ??= await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (user == null)
            return;

        var picture = info.Principal.FindFirstValue("picture");
        if (string.IsNullOrWhiteSpace(picture))
            return;

        await _signInManager.SignOutAsync();
        await _signInManager.SignInWithClaimsAsync(
            user,
            new AuthenticationProperties { IsPersistent = false },
            [new Claim("image", picture)]);
    }

    // **************************************************************************************************************************
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl is "~/" or "/")
            return Redirect("/events");

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect("/events");
    }
}
