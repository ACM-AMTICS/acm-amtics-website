using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers.Api
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardApiController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // GET: /api/dashboard/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetStatsAsync();
            return Ok(stats);
        }
    }
}
