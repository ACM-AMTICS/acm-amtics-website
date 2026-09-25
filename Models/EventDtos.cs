using System.ComponentModel.DataAnnotations;

namespace acm_amtics_website.Models
{
    public class EventCreateDto
    {
        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(150, ErrorMessage = "Event name cannot exceed 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required.")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Event time is required.")]
        public string Time { get; set; } = "10:00 AM - 1:00 PM";

        [Required(ErrorMessage = "Venue is required.")]
        public string Venue { get; set; } = "Seminar Hall, AMTICS";

        [Required(ErrorMessage = "Category is required.")]
        public string Category { get; set; } = "Workshop";

        public string? Description { get; set; }
    }

    public class EventsStatsDto
    {
        public int TotalEvents { get; set; } = 18;
        public string EventsChangePercentage { get; set; } = "+20%";
        public string EventsChangeText { get; set; } = "from last semester";

        public int TotalAttendees { get; set; } = 860;
        public string AttendeesChangePercentage { get; set; } = "+16%";
        public string AttendeesChangeText { get; set; } = "from last semester";
    }

    public class EventAttendeesStatsDto
    {
        public int TotalAttendees { get; set; } = 120;
        public int MembersCount { get; set; } = 86;
        public string MembersPercentage { get; set; } = "71.7% of attendees";

        public int NonMembersCount { get; set; } = 34;
        public string NonMembersPercentage { get; set; } = "28.3% of attendees";

        public string AttendanceRate { get; set; } = "92%";
        public string AttendanceRateChange { get; set; } = "+12% vs last event";
        public bool AttendanceRateChangeIsPositive { get; set; } = true;
    }

    public class EventAttendeesResponseDto
    {
        public EventItem? Event { get; set; }
        public EventAttendeesStatsDto Stats { get; set; } = new();
        public PaginatedResult<AttendanceRecord> Attendees { get; set; } = new();
    }
}
