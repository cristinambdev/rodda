using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    public class HomeController : Controller
    {
        [Route("/index")]
    // **************************************************************************************************************************
        public IActionResult Index()
        {
            ViewData["ActiveNav"] = "home";
            return View();
        }
    }
}
