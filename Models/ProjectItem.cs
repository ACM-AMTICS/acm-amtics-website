using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace acm_amtics_website.Models
{
    public class ProjectTeamMember
    {
        [BsonElement("memberId")]
        public string? MemberId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [BsonElement("role")]
        public string? Role { get; set; }
    }

    public class ProjectItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("slug")]
        public string Slug { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("shortDescription")]
        public string ShortDescription { get; set; } = string.Empty;

        [BsonElement("fullDescription")]
        public string FullDescription { get; set; } = string.Empty;

        [BsonElement("imageUrl")]
        public string? ImageUrl { get; set; }

        [BsonElement("category")]
        public string Category { get; set; } = "Web App"; // Web App, AI/ML, Mobile App, Cybersecurity, Developer Tool, Open Source

        [BsonElement("subCategory")]
        public string? SubCategory { get; set; } // Sustainability, Education, Campus Utility, etc.

        [BsonElement("technologies")]
        public List<string> Technologies { get; set; } = new();

        [BsonElement("teamName")]
        public string TeamName { get; set; } = string.Empty;

        [BsonElement("teamMembers")]
        public List<ProjectTeamMember> TeamMembers { get; set; } = new();

        [BsonElement("year")]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        [BsonElement("githubUrl")]
        public string? GithubUrl { get; set; }

        [BsonElement("liveDemoUrl")]
        public string? LiveDemoUrl { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Completed"; // Completed, In Progress, Active

        [BsonElement("featured")]
        public bool Featured { get; set; } = false;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
