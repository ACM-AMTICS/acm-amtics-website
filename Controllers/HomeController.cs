using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using acm_amtics_website.Models;
using acm_amtics_website.Services;

namespace acm_amtics_website.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IEventService _eventService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IDashboardService dashboardService,
        IEventService eventService,
        ILogger<HomeController> logger)
    {
        _dashboardService = dashboardService;
        _eventService = eventService;
        _logger = logger;
    }

    [HttpGet]
    [Route("")]
    [Route("Home")]
    [Route("Home/Index")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveMenu = "Home";
        var model = new LandingViewModel();

        try
        {
            var stats = await _dashboardService.GetStatsAsync();
            if (stats != null)
            {
                model.ActiveMembersCount = Math.Max(500, stats.Members?.Count ?? 500);
                model.EventsCount = Math.Max(30, stats.Events?.Count ?? 30);
            }

            var events = await _eventService.GetActiveEventsAsync();
            if (events != null && events.Count > 0)
            {
                model.RecentEvents = events.Take(3).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load live landing stats from MongoDB, utilizing resilient fallback values.");
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
