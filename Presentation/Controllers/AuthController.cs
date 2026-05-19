using Business.Services;
using Domain.Extensions;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

public class AuthController(IAuthService authService) : Controller
{
    private readonly IAuthService _authService = authService;


    [HttpGet]
    // **************************************************************************************************************************
    public IActionResult SignIn(string returnUrl = "~/")
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new SignInViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // **************************************************************************************************************************
    public async Task<IActionResult> SignIn(SignInViewModel model, string returnUrl = "~/")
    {
        ViewBag.ErrorMessage = null;
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var signInFormData = model.MapTo<SignInFormData>();

        var result = await _authService.SignInAsync(signInFormData);
        if (!result.Succeeded)
        {
            ViewBag.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        return LocalRedirect(returnUrl);
    }

    [HttpGet]
    // **************************************************************************************************************************
    public IActionResult SignUp()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // **************************************************************************************************************************
    public async Task<IActionResult> SignUp(SignUpViewModel model)
    {
        ViewBag.ErrorMessage = null;

        if (!ModelState.IsValid)
            return View(model);

        var signUpFormData = model.MapTo<SignUpFormData>();

        var result = await _authService.SignUpAsync(signUpFormData);
        if (!result.Succeeded)
        {
            ViewBag.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        return RedirectToAction("SignIn", "Auth");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    // **************************************************************************************************************************
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        return RedirectToAction("SignIn");
    }
}
