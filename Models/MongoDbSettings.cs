namespace acm_amtics_website.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "acm_amtics_admin_db";
        public string UsersCollectionName { get; set; } = "Admins";
        public string MembersCollectionName { get; set; } = "Members";
        public string EventsCollectionName { get; set; } = "Events";
        public string AttendanceCollectionName { get; set; } = "Attendance";
        public string ProjectsCollectionName { get; set; } = "Projects";
    }
}
