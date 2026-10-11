using acm_amtics_website.Models;

namespace acm_amtics_website.Models
{
    public class HomeStatItem
    {
        public string Icon { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class HomeHeroModel
    {
        public string Badge { get; set; } = string.Empty;
        public string TitleLine1 { get; set; } = string.Empty;
        public string TitleLine2 { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ExploreEventsText { get; set; } = string.Empty;
        public string LearnMoreText { get; set; } = string.Empty;
        public string CommunityStatsText { get; set; } = string.Empty;
    }

    public class HomeDataModel
    {
        public HomeHeroModel Hero { get; set; } = new();
        public List<HomeStatItem> Stats { get; set; } = new();
    }

    public class LandingViewModel
    {
        public string ActiveMembersDisplay { get; set; } = "150+";
        public string EventsCountDisplay { get; set; } = "3";
        public string ProjectsCountDisplay { get; set; } = "3";
        public string AwardsCountDisplay { get; set; } = "10+";
        public string CommunityStatsText { get; set; } = "Join 150+ students growing with ACM AMTICS";
        public List<HomeStatItem> Stats { get; set; } = new();
        public List<EventItem> RecentEvents { get; set; } = new();
        public List<ProjectItemModel> FeaturedProjects { get; set; } = new();
    }
}
