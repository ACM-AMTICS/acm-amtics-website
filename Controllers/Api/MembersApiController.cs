using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers.Api
{
    [ApiController]
    [Route("api/members")]
    public class MembersApiController : ControllerBase
    {
        private readonly IMemberService _memberService;
        private readonly IEventService _eventService;
        private readonly ILogger<MembersApiController> _logger;

        public MembersApiController(
            IMemberService memberService, 
            IEventService eventService,
            ILogger<MembersApiController> logger)
        {
            _memberService = memberService;
            _eventService = eventService;
            _logger = logger;
        }

        // GET: /api/members?page=1&pageSize=8&search=
        [HttpGet]
        public async Task<IActionResult> GetMembers([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 8)
        {
            try
            {
                var result = await _memberService.GetMembersAsync(search, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve members list");
                return StatusCode(500, new { message = "Error retrieving members list." });
            }
        }

        // GET: /api/members/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMemberById(string id)
        {
            var member = await _memberService.GetMemberByIdAsync(id);
            if (member == null)
            {
                return NotFound(new { message = "Member not found." });
            }
            return Ok(member);
        }

        // POST: /api/members
        // Protected: Only accessible from Dashboard view context
        [HttpPost]
        public async Task<IActionResult> CreateMember([FromBody] MemberCreateDto dto)
        {
            // Server-side Access Control verification:
            // Ensure request originated from the Dashboard view context via custom header or referer validation
            var contextHeader = Request.Headers["X-View-Context"].ToString();
            var referer = Request.Headers["Referer"].ToString();

            bool isDashboardContext = string.Equals(contextHeader, "Dashboard", StringComparison.OrdinalIgnoreCase) ||
                                      (!string.IsNullOrEmpty(referer) && referer.Contains("/Dashboard", StringComparison.OrdinalIgnoreCase));

            if (!isDashboardContext)
            {
                _logger.LogWarning("Unauthorized attempt to add member outside of Dashboard context. Context: {Header}, Referer: {Referer}", contextHeader, referer);
                return StatusCode(StatusCodes.Status403Forbidden, new 
                { 
                    success = false,
                    message = "Access Denied: The 'Add Member' action is strictly restricted to the Dashboard context only." 
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // If Role requires an assigned event (e.g. Coordinator or non-Member) and event id is given, look up event name
            if (!string.IsNullOrWhiteSpace(dto.AssignedEventId) && string.IsNullOrWhiteSpace(dto.AssignedEventName))
            {
                var events = await _eventService.GetActiveEventsAsync();
                var ev = events.FirstOrDefault(e => e.Id == dto.AssignedEventId);
                if (ev != null)
                {
                    dto.AssignedEventName = ev.Name;
                }
            }

            try
            {
                var createdMember = await _memberService.CreateMemberAsync(dto);
                return CreatedAtAction(nameof(GetMemberById), new { id = createdMember.Id }, createdMember);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create new member");
                return StatusCode(500, new { success = false, message = "Database error while adding member." });
            }
        }

        // DELETE: /api/members/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMember(string id)
        {
            var success = await _memberService.DeleteMemberAsync(id);
            if (!success)
            {
                return NotFound(new { message = "Member not found or could not be deleted." });
            }
            return Ok(new { success = true, message = "Member successfully removed." });
        }
    }
}
