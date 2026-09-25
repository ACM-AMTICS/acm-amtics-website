using acm_amtics_website.Services;
using Microsoft.AspNetCore.Authorization;
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
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Events";
            ViewBag.ShowAddEventButton = true; // Server-side enforced: exposed ONLY on Events & Attendance list page
            ViewBag.ViewContext = "Events";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View();
        }

        // GET: /Events/{id}/Attendees
        [HttpGet]
        [Route("Events/{id}/Attendees")]
        public async Task<IActionResult> Attendees(string id)
        {
            var eventItem = await _eventService.GetEventByIdAsync(id);
            if (eventItem == null)
            {
                return RedirectToAction(nameof(Index));
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
