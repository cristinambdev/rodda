using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Authorize]
public class SettingsController : Controller
{
    // **************************************************************************************************************************
    [HttpGet]
    [Route("/settings")]
    public IActionResult Index()
    {
        ViewData["ActiveNav"] = "profile";
        return View();
    }
}
