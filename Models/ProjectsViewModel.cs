namespace acm_amtics_website.Models
{
    public class ProjectsViewModel
    {
        public List<ProjectItem> Projects { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

        // Active filters
        public string? SearchQuery { get; set; }
        public string? SelectedCategory { get; set; }
        public string? SelectedTechnology { get; set; }
        public int? SelectedYear { get; set; }
        public string? SortBy { get; set; } = "recent";

        // Available filter options
        public List<string> AvailableCategories { get; set; } = new();
        public List<string> AvailableTechnologies { get; set; } = new();
        public List<int> AvailableYears { get; set; } = new();
    }
}
