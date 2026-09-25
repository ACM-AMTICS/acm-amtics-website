using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace acm_amtics_website.Models
{
    public class Member
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [BsonElement("countryCode")]
        public string CountryCode { get; set; } = "+91";

        [BsonElement("enrollmentNumber")]
        public string EnrollmentNumber { get; set; } = string.Empty;

        [BsonElement("role")]
        public string Role { get; set; } = "Member"; // Member, Coordinator, Event Head, Technical Head, Vice President, President

        [BsonElement("assignedEventId")]
        public string? AssignedEventId { get; set; }

        [BsonElement("assignedEventName")]
        public string? AssignedEventName { get; set; }

        [BsonElement("joinDate")]
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        [BsonElement("status")]
        public string Status { get; set; } = "Active"; // Active, Inactive

        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [BsonElement("orderIndex")]
        public int OrderIndex { get; set; } = 999;

        [BsonIgnore]
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return "M";
                var parts = Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                }
                return Name.Substring(0, Math.Min(2, Name.Length)).ToUpper();
            }
        }
    }
}
