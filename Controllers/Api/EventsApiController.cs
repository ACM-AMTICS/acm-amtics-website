using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers.Api
{
    [ApiController]
    [Route("api/events")]
    public class EventsApiController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<EventsApiController> _logger;

        public EventsApiController(
            IEventService eventService,
            IAttendanceService attendanceService,
            ILogger<EventsApiController> logger)
        {
            _eventService = eventService;
            _attendanceService = attendanceService;
            _logger = logger;
        }

        // GET: /api/events?search=...&page=1&pageSize=8
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 8)
        {
            try
            {
                var result = await _eventService.GetEventsAsync(search, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve events");
                return StatusCode(500, new { message = "An error occurred while fetching events." });
            }
        }

        // GET: /api/events/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetEventsStats()
        {
            try
            {
                var stats = await _eventService.GetEventsStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve events statistics");
                return StatusCode(500, new { message = "An error occurred while fetching event statistics." });
            }
        }

        // GET: /api/events/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(string id)
        {
            try
            {
                var ev = await _eventService.GetEventByIdAsync(id);
                if (ev == null)
                {
                    return NotFound(new { message = $"Event with ID '{id}' was not found." });
                }
                return Ok(ev);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve event {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving event details." });
            }
        }

        // POST: /api/events
        [HttpPost]
        public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var user = User.Identity?.Name ?? "Admin";
                var created = await _eventService.CreateEventAsync(dto, user);
                return CreatedAtAction(nameof(GetEventById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create event");
                return StatusCode(500, new { message = "An error occurred while creating the event." });
            }
        }

        // PUT: /api/events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(string id, [FromBody] EventCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var success = await _eventService.UpdateEventAsync(id, dto);
                if (!success)
                {
                    return NotFound(new { message = $"Event with ID '{id}' was not found." });
                }
                return Ok(new { message = "Event updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update event {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the event." });
            }
        }

        // DELETE: /api/events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(string id)
        {
            try
            {
                var success = await _eventService.DeleteEventAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Event with ID '{id}' was not found." });
                }
                return Ok(new { message = "Event deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete event {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the event." });
            }
        }

        // GET: /api/events/{id}/attendees?search=...&page=1&pageSize=8
        [HttpGet("{id}/attendees")]
        public async Task<IActionResult> GetAttendees(string id, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 8)
        {
            try
            {
                var ev = await _eventService.GetEventByIdAsync(id);
                if (ev == null)
                {
                    return NotFound(new { message = $"Event with ID '{id}' was not found." });
                }

                var stats = await _attendanceService.GetEventAttendeesStatsAsync(id);
                var attendees = await _attendanceService.GetEventAttendeesAsync(id, search, page, pageSize);

                var response = new EventAttendeesResponseDto
                {
                    Event = ev,
                    Stats = stats,
                    Attendees = attendees
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve attendees for event {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving attendees." });
            }
        }

        // GET: /api/events/{id}/attendees/export?search=...
        [HttpGet("{id}/attendees/export")]
        public async Task<IActionResult> ExportAttendees(string id, [FromQuery] string? search)
        {
            try
            {
                var ev = await _eventService.GetEventByIdAsync(id);
                var eventName = ev != null ? ev.Name.Replace(" ", "_") : "Event";
                var csvBytes = await _attendanceService.ExportAttendeesCsvAsync(id, search);

                var fileName = $"{eventName}_Attendees_{DateTime.UtcNow:yyyyMMdd}.csv";
                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export attendees for event {Id}", id);
                return StatusCode(500, new { message = "An error occurred while exporting attendee data." });
            }
        }
    }
}
