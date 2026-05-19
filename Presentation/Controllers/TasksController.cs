using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    public class TasksController : Controller
    {
        // **************************************************************************************************************************
        public IActionResult Tasks()
        {
            return View();
        }

        // **************************************************************************************************************************
        public IActionResult Items()
        {
            return View();
        }
    }
}
