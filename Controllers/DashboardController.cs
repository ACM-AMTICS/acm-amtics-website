using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IMongoDbContext _mongoDbContext;

        public DashboardController(IDashboardService dashboardService, IMongoDbContext mongoDbContext)
        {
            _dashboardService = dashboardService;
            _mongoDbContext = mongoDbContext;
        }

        [HttpGet]
        [Route("Dashboard")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Coordinator") || string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, "Coordinator", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Events");
            }

            ViewBag.ActiveMenu = "Dashboard";
            ViewBag.ShowAddMemberButton = true;
            ViewBag.ViewContext = "Dashboard";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            ViewBag.MongoDatabase = _mongoDbContext.DatabaseName;

            var stats = await _dashboardService.GetStatsAsync();
            return View(stats);
        }
    }
}
