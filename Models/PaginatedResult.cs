namespace acm_amtics_website.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public class DashboardStatsDto
    {
        public StatItem Members { get; set; } = new();
        public StatItem Events { get; set; } = new();
        public StatItem Attendees { get; set; } = new();
        public StatItem Projects { get; set; } = new();
    }

    public class StatItem
    {
        public int Count { get; set; }
        public string ChangePercentage { get; set; } = "+0%";
        public bool IsPositive { get; set; } = true;
        public string Label { get; set; } = string.Empty;
    }
}
