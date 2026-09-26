using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace acm_amtics_website.Models
{
    public class EventItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [BsonElement("time")]
        public string Time { get; set; } = "10:00 AM - 1:00 PM";

        [BsonElement("venue")]
        public string Venue { get; set; } = "Seminar Hall, AMTICS";

        [BsonElement("category")]
        public string Category { get; set; } = "Workshop"; // Workshop, Seminar, Competition, Talk

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; } = "Admin";

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("status")]
        public string Status { get; set; } = "Upcoming"; // Upcoming, Active, Completed

        [BsonElement("attendeesCount")]
        public int AttendeesCount { get; set; } = 0;

        [BsonElement("orderIndex")]
        public int OrderIndex { get; set; } = 0;

        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }

        [BsonIgnore]
        public bool IsToday => Date.Date == DateTime.UtcNow.Date || Date.Date == DateTime.Today || (Date.Month == DateTime.UtcNow.Month && Date.Day == DateTime.UtcNow.Day);

        [BsonIgnore]
        public bool HasActiveQr => IsToday || Status == "Active" || OrderIndex == 1;
    }
}
