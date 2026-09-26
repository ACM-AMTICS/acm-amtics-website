using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class EventsController : Controller
    {
        private readonly IMongoDbContext _mongoDbContext;
        private readonly IEventService _eventService;

        public EventsController(IMongoDbContext mongoDbContext, IEventService eventService)
        {
            _mongoDbContext = mongoDbContext;
            _eventService = eventService;
        }

        // GET: /Events
        [HttpGet]
        [Route("Events")]
        [Route("Events/Index")]
        public async Task<IActionResult> Index(
            [FromQuery] string? search = null,
            [FromQuery] string? type = null,
            [FromQuery] int? year = null,
            [FromQuery] string? sortBy = "recent",
            [FromQuery] string? tab = "completed",
            [FromQuery] int page = 1,
            [FromQuery] string? view = null)
        {
            // If explicit admin management requested or coordinator redirected without public flag
            if (string.Equals(view, "admin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(view, "manage", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Manage));
            }

            if (User.Identity?.IsAuthenticated == true &&
                string.IsNullOrEmpty(search) &&
                string.IsNullOrEmpty(type) &&
                !year.HasValue &&
                !Request.Query.ContainsKey("tab") &&
                !Request.Query.ContainsKey("public"))
            {
                return RedirectToAction(nameof(Manage));
            }

            ViewBag.ActiveMenu = "Events";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;

            var viewModel = await _eventService.GetPublicEventsAsync(search, type, year, sortBy, tab, page, pageSize: 8);
            return View("Index", viewModel);
        }

        // GET: /Events/Manage (Admin Events & Attendance page)
        [HttpGet]
        [Route("Events/Manage")]
        public IActionResult Manage()
        {
            ViewBag.ActiveMenu = "Events";
            ViewBag.ShowAddEventButton = true; // Server-side enforced: exposed ONLY on Events & Attendance list page
            ViewBag.ViewContext = "Events";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View("Manage");
        }

        // GET: /Events/Details/{id}
        [HttpGet]
        [Route("Events/Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var eventItem = await _eventService.GetEventByIdAsync(id);
            if (eventItem == null)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ActiveMenu = "Events";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View(eventItem);
        }

        // GET: /Events/{id}/Attendees
        [HttpGet]
        [Route("Events/{id}/Attendees")]
        public async Task<IActionResult> Attendees(string id)
        {
            var eventItem = await _eventService.GetEventByIdAsync(id);
            if (eventItem == null)
            {
                return RedirectToAction(nameof(Manage));
            }

            ViewBag.ActiveMenu = "Events";
            ViewBag.ShowAddEventButton = false; // Server-side enforced: NOT exposed on Attendees page
            ViewBag.ViewContext = "Attendees";
            ViewBag.EventId = id;
            ViewBag.Event = eventItem;
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View(eventItem);
        }
    }
}
