using acm_amtics_website.Models;
using MongoDB.Driver;

namespace acm_amtics_website.Services
{
    public interface IMongoDbContext
    {
        IMongoCollection<Member>? MembersCollection { get; }
        IMongoCollection<EventItem>? EventsCollection { get; }
        IMongoCollection<AttendanceRecord>? AttendanceCollection { get; }
        IMongoCollection<ProjectItem>? ProjectsCollection { get; }
        IMongoCollection<User>? AdminsCollection { get; }
        bool IsConnected { get; }
        string ConnectionString { get; }
        string DatabaseName { get; }
    }
}
