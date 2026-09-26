namespace acm_amtics_website.Models
{
    public class EventsViewModel
    {
        public List<EventItem> Events { get; set; } = new();
        public string? SearchQuery { get; set; }
        public string? SelectedType { get; set; }
        public int? SelectedYear { get; set; }
        public string SortBy { get; set; } = "recent";
        public string ActiveTab { get; set; } = "completed"; // "completed" or "upcoming"
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public int CompletedCount { get; set; } = 0;
        public int UpcomingCount { get; set; } = 0;
        public List<string> AvailableTypes { get; set; } = new() { "Workshop", "Seminar", "Competition", "Talk", "Hackathon" };
        public List<int> AvailableYears { get; set; } = new() { 2024, 2025, 2026 };
    }
}
