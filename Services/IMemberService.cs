using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public interface IMemberService
    {
        Task<PaginatedResult<Member>> GetMembersAsync(string? search, int page = 1, int pageSize = 8);
        Task<Member?> GetMemberByIdAsync(string id);
        Task<Member> CreateMemberAsync(MemberCreateDto dto);
        Task<Member?> UpdateMemberAsync(string id, MemberCreateDto dto);
        Task<bool> DeleteMemberAsync(string id);
        Task<int> GetTotalMembersCountAsync();
    }
}
