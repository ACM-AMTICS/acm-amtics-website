using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace acm_amtics_website.Models
{
    public class AttendanceRecord
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("eventId")]
        public string EventId { get; set; } = string.Empty;

        [BsonElement("attendeeName")]
        public string AttendeeName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("enrollmentNumber")]
        public string? EnrollmentNumber { get; set; }

        [BsonElement("type")]
        public string Type { get; set; } = "Undefined"; // "Member" or "Undefined"

        [BsonElement("status")]
        public string Status { get; set; } = "Present"; // "Present" or "Absent"

        [BsonElement("joinedAt")]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [BsonElement("handle")]
        public string? Handle { get; set; }

        [BsonIgnore]
        public string JoinedAtFormatted => JoinedAt.ToString("hh:mm tt");

        [BsonIgnore]
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AttendeeName)) return "ST";
                var parts = AttendeeName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
                return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
            }
        }
    }
}
