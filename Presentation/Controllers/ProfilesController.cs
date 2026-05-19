using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    public class ProfilesController : Controller
    {
        // **************************************************************************************************************************
        public IActionResult Index()
        {
            return View();
        }

        // **************************************************************************************************************************
        public IActionResult MyEvents()
        {
            return View();
        }
    }
}
