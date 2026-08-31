using Microsoft.AspNetCore.Mvc;

namespace FixMyCity.Web.Controllers
{
    public class MapController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}