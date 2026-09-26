using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IMongoDbContext _mongoDbContext;
        private readonly IProjectService _projectService;

        public ProjectsController(IMongoDbContext mongoDbContext, IProjectService projectService)
        {
            _mongoDbContext = mongoDbContext;
            _projectService = projectService;
        }

        // GET: /Projects
        [HttpGet]
        [Route("Projects")]
        [Route("Projects/Index")]
        public async Task<IActionResult> Index(
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] string? technology = null,
            [FromQuery] int? year = null,
            [FromQuery] string? sortBy = "recent",
            [FromQuery] int page = 1)
        {
            ViewBag.ActiveMenu = "Projects";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;

            var viewModel = await _projectService.GetPublicProjectsAsync(search, category, technology, year, sortBy, page, pageSize: 8);
            return View("Index", viewModel);
        }

        // GET: /Projects/Details/{id}
        // GET: /Projects/{id}
        [HttpGet]
        [Route("Projects/Details/{id}")]
        [Route("Projects/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Index));
            }

            var project = await _projectService.GetProjectByIdOrSlugAsync(id);
            if (project == null)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ActiveMenu = "Projects";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View("Details", project);
        }
    }
}
