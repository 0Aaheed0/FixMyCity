using Microsoft.AspNetCore.Mvc;

namespace FixMyCity.Web.Controllers
{
    public class ProfileController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}