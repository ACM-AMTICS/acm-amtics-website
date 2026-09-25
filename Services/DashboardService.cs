using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IMemberService _memberService;
        private readonly IEventService _eventService;

        public DashboardService(IMemberService memberService, IEventService eventService)
        {
            _memberService = memberService;
            _eventService = eventService;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var memberCount = await _memberService.GetTotalMembersCountAsync();
            var eventCount = await _eventService.GetTotalEventsCountAsync();

            return new DashboardStatsDto
            {
                Members = new StatItem
                {
                    Count = memberCount > 0 ? memberCount : 124,
                    ChangePercentage = "+12%",
                    IsPositive = true,
                    Label = "from last month"
                },
                Events = new StatItem
                {
                    Count = eventCount > 0 ? eventCount : 18,
                    ChangePercentage = "+20%",
                    IsPositive = true,
                    Label = "from last month"
                },
                Attendees = new StatItem
                {
                    Count = 860,
                    ChangePercentage = "+16%",
                    IsPositive = true,
                    Label = "from last month"
                },
                Projects = new StatItem
                {
                    Count = 12,
                    ChangePercentage = "+33%",
                    IsPositive = true,
                    Label = "from last month"
                }
            };
        }
    }
}
