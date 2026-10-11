namespace acm_amtics_website.Models;

public class GalleryItemModel
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string DisplayDate { get; set; } = string.Empty;
    public string Count { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
}

public class GalleryReelModel
{
    public string Title { get; set; } = string.Empty;
    public string AriaLabel { get; set; } = string.Empty;
    public string Img { get; set; } = string.Empty;
    public string ImgAlt { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class GalleryViewModel
{
    public List<GalleryItemModel> Items { get; set; } = new();
    public List<GalleryReelModel> Reels { get; set; } = new();
    public List<string> Categories { get; set; } = new();
}

public class ProjectItemModel
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Tech { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string GithubUrl { get; set; } = string.Empty;
}

public class ProjectsViewModel
{
    public List<ProjectItemModel> Items { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Technologies { get; set; } = new();
    public List<string> Years { get; set; } = new();
}

public class TeamMemberModel
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string LinkedinUrl { get; set; } = string.Empty;
    public string GithubUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;
}

public class TeamSectionModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string ViewAllText { get; set; } = string.Empty;
    public string ViewAllLink { get; set; } = string.Empty;
    public string ViewPastText { get; set; } = string.Empty;
    public string ViewPastLink { get; set; } = string.Empty;
    public List<TeamMemberModel> Members { get; set; } = new();
}

public class TeamViewModel
{
    public TeamSectionModel CurrentTeam { get; set; } = new();
    public TeamSectionModel Contributors { get; set; } = new();
    public TeamSectionModel PreviousHeads { get; set; } = new();
}
