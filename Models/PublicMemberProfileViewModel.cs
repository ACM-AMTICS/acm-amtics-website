namespace acm_amtics_website.Models
{
    public class PublicMemberProfileViewModel
    {
        public Member Member { get; set; } = new();
        public List<ProjectItem> Projects { get; set; } = new();
        public string Department { get; set; } = "Computer Science & Engineering";
        public string Bio { get; set; } = "Passionate student technologist and active contributor at the ACM AMTICS Student Chapter.";
        public List<string> Skills { get; set; } = new() { "Full Stack Development", "Python", "Cloud & DevOps", "Problem Solving" };
    }
}
