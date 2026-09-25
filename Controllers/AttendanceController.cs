using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class AttendanceController : Controller
    {
        [HttpGet]
        [Route("Attendance")]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Attendance";
            return View();
        }
    }
}
