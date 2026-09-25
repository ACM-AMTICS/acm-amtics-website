using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public interface IEventService
    {
        Task<PaginatedResult<EventItem>> GetEventsAsync(string? search, int page = 1, int pageSize = 8);
        Task<EventItem?> GetEventByIdAsync(string id);
        Task<EventItem> CreateEventAsync(EventCreateDto dto, string createdBy = "Admin");
        Task<bool> UpdateEventAsync(string id, EventCreateDto dto);
        Task<bool> DeleteEventAsync(string id);
        Task<EventsStatsDto> GetEventsStatsAsync();
        Task<List<EventItem>> GetActiveEventsAsync();
        Task<int> GetTotalEventsCountAsync();
    }

    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetStatsAsync();
    }
}
