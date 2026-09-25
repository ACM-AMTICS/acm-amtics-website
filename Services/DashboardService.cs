using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IMemberService _memberService;
        private readonly IEventService _eventService;
        private readonly IAttendanceService _attendanceService;

        public DashboardService(IMemberService memberService, IEventService eventService, IAttendanceService attendanceService)
        {
            _memberService = memberService;
            _eventService = eventService;
            _attendanceService = attendanceService;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var memberCount = await _memberService.GetTotalMembersCountAsync();
            var eventCount = await _eventService.GetTotalEventsCountAsync();
            var attendeeCount = await _attendanceService.GetTotalAttendeesCountAsync();

            return new DashboardStatsDto
            {
                Members = new StatItem
                {
                    Count = memberCount,
                },
                Events = new StatItem
                {
                    Count = eventCount,
                },
                Attendees = new StatItem
                {
                    Count = attendeeCount,
                },
            };
        }
    }
}
