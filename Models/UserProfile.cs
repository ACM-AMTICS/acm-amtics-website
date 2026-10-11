using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace acm_amtics_website.Models
{
    public class UserProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("slug")]
        public string Slug { get; set; } = string.Empty; // Email with @gmail.com or @domain removed

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("department")]
        public string Department { get; set; } = "Computer Science & Engineering";

        [BsonElement("academicYear")]
        public string AcademicYear { get; set; } = "3rd Year";

        [BsonElement("college")]
        public string College { get; set; } = "AMTICS, Gandhinagar";

        [BsonElement("bio")]
        public string Bio { get; set; } = "Passionate about design, technology and community building. Excited to learn, collaborate and create meaningful impact with ACM AMTICS.";

        [BsonElement("quote")]
        public string Quote { get; set; } = "Ideas grow when shared.";

        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }

        [BsonElement("memberSince")]
        public string MemberSince { get; set; } = DateTime.UtcNow.ToString("MMM yyyy");

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public static string GenerateSlug(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return string.Empty;
            var clean = email.Trim().ToLowerInvariant();
            if (clean.EndsWith("@gmail.com"))
            {
                return clean.Substring(0, clean.Length - "@gmail.com".Length);
            }
            if (clean.Contains("@"))
            {
                return clean.Split('@')[0];
            }
            return clean;
        }
    }

    public class BadgeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FullDesc { get; set; } = string.Empty;
        public string ColorType { get; set; } = "purple";
        public string Grad1 { get; set; } = "#8B5CF6";
        public string Grad2 { get; set; } = "#6D28D9";
        public string Icon { get; set; } = string.Empty;
        public bool Earned { get; set; }
        public string ProgressText { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public UserProfile Profile { get; set; } = new UserProfile();
        public Member? MemberRecord { get; set; }
        public List<BadgeDto> Badges { get; set; } = new List<BadgeDto>();
        public List<EventItem> AttendedEvents { get; set; } = new List<EventItem>();
        public List<MemberCoordinatorRoleDto> CoordinatorRoles { get; set; } = new List<MemberCoordinatorRoleDto>();
        public bool IsOwnProfile { get; set; }
        public string FormGoogleUrl { get; set; } = "https://forms.google.com"; // Placeholder URL
    }

    public class MemberCoordinatorRoleDto
    {
        public string EventId { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string RoleTitle { get; set; } = "Coordinator";
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public string StatusClass { get; set; } = "role-badge-current";
        public string Image { get; set; } = "/images/events/ui-ux-workshop.jpg";
    }

    public class EditProfileDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
        public string Department { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Academic Year cannot exceed 50 characters.")]
        public string AcademicYear { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string Bio { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Quote cannot exceed 200 characters.")]
        public string Quote { get; set; } = string.Empty;
    }
}
