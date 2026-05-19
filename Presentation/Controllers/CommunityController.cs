using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers
{
    public class CommunityController : Controller
    {
        // **************************************************************************************************************************
        public IActionResult Community()
        {
            return View();
        }
    }
}
