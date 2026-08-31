using Microsoft.AspNetCore.Mvc;

namespace FixMyCity.Web.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Departments()
        {
            return View();
        }

        public IActionResult Categories()
        {
            return View();
        }

        public IActionResult Analytics()
        {
            return View();
        }
    }
}