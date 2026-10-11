using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using acm_amtics_website.Models;
using acm_amtics_website.Services;

namespace acm_amtics_website.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IEventService _eventService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IDashboardService dashboardService,
        IEventService eventService,
        IWebHostEnvironment webHostEnvironment,
        ILogger<HomeController> logger)
    {
        _dashboardService = dashboardService;
        _eventService = eventService;
        _webHostEnvironment = webHostEnvironment;
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

        // Load stats & content from homeData.json
        try
        {
            var homeData = LoadJsonData<HomeDataModel>("homeData.json");
            if (homeData != null)
            {
                if (homeData.Stats != null && homeData.Stats.Count > 0)
                {
                    model.Stats = homeData.Stats;
                    
                    var memberStat = homeData.Stats.FirstOrDefault(s => s.Label.Contains("Member", StringComparison.OrdinalIgnoreCase));
                    if (memberStat != null)
                    {
                        model.ActiveMembersDisplay = memberStat.Number;
                    }

                    var eventStat = homeData.Stats.FirstOrDefault(s => s.Label.Contains("Event", StringComparison.OrdinalIgnoreCase));
                    if (eventStat != null)
                    {
                        model.EventsCountDisplay = eventStat.Number;
                    }

                    var projStat = homeData.Stats.FirstOrDefault(s => s.Label.Contains("Project", StringComparison.OrdinalIgnoreCase));
                    if (projStat != null)
                    {
                        model.ProjectsCountDisplay = projStat.Number;
                    }

                    var awardStat = homeData.Stats.FirstOrDefault(s => s.Label.Contains("Award", StringComparison.OrdinalIgnoreCase));
                    if (awardStat != null)
                    {
                        model.AwardsCountDisplay = awardStat.Number;
                    }
                }

                if (!string.IsNullOrWhiteSpace(homeData.Hero?.CommunityStatsText))
                {
                    model.CommunityStatsText = homeData.Hero.CommunityStatsText;
                }
                else
                {
                    model.CommunityStatsText = $"Join {model.ActiveMembersDisplay} students growing with ACM AMTICS";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load homeData.json for landing page.");
        }

        try
        {
            var events = await _eventService.GetActiveEventsAsync();
            if (events != null && events.Count > 0)
            {
                model.RecentEvents = events.Take(3).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load active events for landing page.");
        }

        try
        {
            var projectsData = LoadJsonData<ProjectsViewModel>("projectsData.json");
            if (projectsData?.Items != null && projectsData.Items.Count > 0)
            {
                model.FeaturedProjects = projectsData.Items;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load projectsData.json for landing page.");
        }

        return View(model);
    }

    [HttpGet]
    [Route("Projects")]
    public IActionResult Projects()
    {
        ViewBag.ActiveMenu = "Projects";
        var model = LoadJsonData<ProjectsViewModel>("projectsData.json") ?? new ProjectsViewModel();
        return View(model);
    }

    [HttpGet]
    [Route("Gallery")]
    public IActionResult Gallery()
    {
        ViewBag.ActiveMenu = "Gallery";
        var model = LoadJsonData<GalleryViewModel>("galleryData.json") ?? new GalleryViewModel();
        return View(model);
    }

    [HttpGet]
    [Route("Heads")]
    [Route("Team")]
    public IActionResult Team()
    {
        ViewBag.ActiveMenu = "Heads";
        var model = LoadJsonData<TeamViewModel>("teamData.json") ?? new TeamViewModel();
        return View(model);
    }

    [HttpGet]
    [Route("Contributors")]
    public IActionResult Contributors()
    {
        ViewBag.ActiveMenu = "Contributors";
        return View();
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

    private T? LoadJsonData<T>(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "data", fileName);
            if (System.IO.File.Exists(filePath))
            {
                var json = System.IO.File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<T>(json, options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading JSON file {FileName}", fileName);
        }
        return default;
    }
}
