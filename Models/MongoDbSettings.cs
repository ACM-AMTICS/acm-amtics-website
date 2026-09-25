namespace acm_amtics_website.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "acm_amtics_db";
        public string UsersCollectionName { get; set; } = "users";
    }
}
