using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    public class CommunityController : Controller
    {
        // **************************************************************************************************************************
        [HttpGet("/community")]
        public IActionResult Community()
        {
            ViewData["ActiveNav"] = "community";
            return View();
        }
    }
}
