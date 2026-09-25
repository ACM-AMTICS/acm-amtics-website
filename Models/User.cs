using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace acm_amtics_website.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("fullName")]
        public string FullName { get; set; } = "ACM Member";

        [BsonElement("role")]
        public string Role { get; set; } = "Admin"; // Admin, Coordinator

        [BsonElement("assignedEventId")]
        public string? AssignedEventId { get; set; }

        [BsonElement("assignedEventName")]
        public string? AssignedEventName { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }
    }
}
