using acm_amtics_website.Models;

namespace acm_amtics_website.Models
{
    public class LandingViewModel
    {
        public int ActiveMembersCount { get; set; } = 500;
        public int EventsCount { get; set; } = 30;
        public int ProjectsCount { get; set; } = 60;
        public int AwardsCount { get; set; } = 10;
        public List<EventItem> RecentEvents { get; set; } = new();
    }
}
