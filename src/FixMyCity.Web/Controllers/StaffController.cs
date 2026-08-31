using Microsoft.AspNetCore.Mvc;

namespace FixMyCity.Web.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        // GET: Staff/Evidence/5
        public IActionResult Evidence(int id)
        {
            ViewBag.IssueId = id;
            return View();
        }
    }
}