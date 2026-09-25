using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public interface IUserService
    {
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ValidatePasswordAsync(string email, string password);
        Task<User> CreateUserAsync(string email, string password, string fullName = "ACM Member");
        Task UpsertCoordinatorUserAsync(string email, string fullName, string? assignedEventId, string? assignedEventName);
        Task UpdateLastLoginAsync(string id);
        Task SeedDefaultAdminAsync();
    }
}
