using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    public class ProfilesController : Controller
    {
        // **************************************************************************************************************************
        public IActionResult Index()
        {
            return RedirectToAction("Community", "Community");
        }

        // **************************************************************************************************************************
        public IActionResult MyEvents()
        {
            ViewData["ActiveNav"] = "events-mine";
            return View();
        }
    }
}
