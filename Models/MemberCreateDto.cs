using System.ComponentModel.DataAnnotations;

namespace acm_amtics_website.Models
{
    public class MemberCreateDto
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must be a valid 10-digit number.")]
        public string Phone { get; set; } = string.Empty;

        public string CountryCode { get; set; } = "+91";

        [Required(ErrorMessage = "Enrollment number is required.")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Enrollment number must be between 3 and 30 characters.")]
        public string EnrollmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = "Member"; // Member, Coordinator, Event Head, Technical Head, Vice President, President

        public string? AssignedEventId { get; set; }
        public string? AssignedEventName { get; set; }

        public string Status { get; set; } = "Active";
    }
}
