using Business.Services;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

public class AuthController(IAuthService authService) : Controller
{
    private readonly IAuthService _authService = authService;

    // **************************************************************************************************************************
    [HttpGet]
    public IActionResult SignIn(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/events");

        ViewBag.ReturnUrl = returnUrl ?? "/index";
        return View(new SignInViewModel());
    }

    // **************************************************************************************************************************
    [HttpPost]
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
    [HttpGet]
    public IActionResult SignUp()
    {
        return View(new SignUpViewModel());
    }

    // **************************************************************************************************************************
    [HttpPost]
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        return RedirectToAction("SignIn");
    }

    // **************************************************************************************************************************
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl is "~/" or "/")
            return Redirect("/events");

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect("/events");
    }
}
