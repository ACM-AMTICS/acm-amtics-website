using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public interface IProjectService
    {
        Task<ProjectsViewModel> GetPublicProjectsAsync(
            string? search = null,
            string? category = null,
            string? technology = null,
            int? year = null,
            string? sortBy = "recent",
            int page = 1,
            int pageSize = 8);

        Task<ProjectItem?> GetProjectByIdOrSlugAsync(string idOrSlug);
        Task<List<ProjectItem>> GetProjectsByMemberIdAsync(string memberId);
        Task<List<ProjectItem>> GetFeaturedProjectsAsync(int count = 3);
        Task<int> GetTotalProjectsCountAsync();
    }
}
