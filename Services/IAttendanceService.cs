using acm_amtics_website.Models;

namespace acm_amtics_website.Services
{
    public interface IAttendanceService
    {
        Task<PaginatedResult<AttendanceRecord>> GetEventAttendeesAsync(string eventId, string? search, int page = 1, int pageSize = 8);
        Task<EventAttendeesStatsDto> GetEventAttendeesStatsAsync(string eventId);
        Task<(string Type, string? EnrollmentNo)> ClassifyAttendeeMemberAsync(string email, string? enrollmentNo);
        Task<byte[]> ExportAttendeesCsvAsync(string eventId, string? search);
        Task<AttendanceRecord> AddAttendanceRecordAsync(AttendanceRecord record);
        Task<List<AttendanceRecord>> GetAttendanceForEventAsync(string eventId);
        Task<int> GetTotalAttendeesCountAsync();
    }
}
