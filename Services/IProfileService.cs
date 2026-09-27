using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public interface IProfileService
    {
        Task<UserProfile?> GetProfileByEmailAsync(string email);
        Task<UserProfile?> GetProfileBySlugAsync(string slug);
        Task<UserProfile> EnsureProfileCreatedAsync(string email, string? name = null);
        Task<UserProfile?> UpdateProfileAsync(string email, EditProfileDto dto);
        Task<ProfileViewModel> GetProfileViewModelAsync(string email, bool isOwnProfile);
        Task<ProfileViewModel?> GetProfileViewModelBySlugAsync(string slug, bool isOwnProfile);
    }
}
