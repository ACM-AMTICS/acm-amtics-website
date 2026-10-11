using acm_amtics_website.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace acm_amtics_website.Services
{
    public class MongoDbContext : IMongoDbContext
    {
        private readonly ILogger<MongoDbContext> _logger;
        private readonly IMongoDatabase? _database;
        private readonly bool _isConnected;
        private readonly string _connectionString;
        private readonly string _databaseName;

        public MongoDbContext(IOptions<MongoDbSettings> settings, IConfiguration configuration, ILogger<MongoDbContext> logger)
        {
            _logger = logger;

            var rawConnectionString = (Environment.GetEnvironmentVariable("MONGODB_URI")
                                       ?? configuration["MongoDB:ConnectionString"]
                                       ?? settings.Value.ConnectionString
                                       ?? string.Empty).Trim().Trim('"', '\'');

            _databaseName = (Environment.GetEnvironmentVariable("MONGODB_DB_NAME")
                            ?? configuration["MongoDB:DatabaseName"]
                            ?? settings.Value.DatabaseName
                            ?? "acm_amtics_admin_db").Trim().Trim('"', '\'');

            if (string.IsNullOrWhiteSpace(_databaseName))
            {
                _databaseName = "acm_amtics_admin_db";
            }

            MongoClient? successfulClient = null;

            if (!string.IsNullOrWhiteSpace(rawConnectionString))
            {
                try
                {
                    var mongoUrl = new MongoUrl(rawConnectionString);
                    var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
                    clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
                    clientSettings.ConnectTimeout = TimeSpan.FromSeconds(3);

                    var client = new MongoClient(clientSettings);
                    var db = client.GetDatabase(_databaseName);

                    // Test connectivity with ping command
                    var pingTask = db.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                    if (pingTask.Wait(3000))
                    {
                        _isConnected = true;
                        successfulClient = client;
                        _database = db;
                        _logger.LogInformation("Successfully connected to MongoDB database '{DatabaseName}'", _databaseName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("MongoDB connection attempt failed: {Message}", ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("No MongoDB connection string configured. Running in resilient in-memory fallback mode.");
            }

            if (!_isConnected || successfulClient == null)
            {
                _logger.LogWarning("MongoDB could not be reached. Running in resilient in-memory fallback mode.");
            }

            _connectionString = SanitizeConnectionString(rawConnectionString);

            var membersCol = settings.Value.MembersCollectionName ?? "Members";
            var eventsCol = settings.Value.EventsCollectionName ?? "Events";
            var attendanceCol = settings.Value.AttendanceCollectionName ?? "Attendance";
            var adminsCol = settings.Value.UsersCollectionName ?? "Admins";

            if (_isConnected && _database != null)
            {
                MembersCollection = _database.GetCollection<Member>(membersCol);
                EventsCollection = _database.GetCollection<EventItem>(eventsCol);
                AttendanceCollection = _database.GetCollection<AttendanceRecord>(attendanceCol);
                AdminsCollection = _database.GetCollection<User>(adminsCol);
                ProfilesCollection = _database.GetCollection<UserProfile>("UserProfiles");
            }
        }

        private static string SanitizeConnectionString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            try
            {
                var url = new MongoUrl(raw);
                if (string.IsNullOrEmpty(url.Username)) return raw;
                // Mask password if present
                var serverPart = string.Join(",", url.Servers.Select(s => $"{s.Host}:{s.Port}"));
                var isSrv = raw.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase);
                return $"mongodb{(isSrv ? "+srv" : "")}://{url.Username}:******@{serverPart}/{url.DatabaseName}";
            }
            catch
            {
                return "mongodb://[configured]";
            }
        }

        public IMongoCollection<Member>? MembersCollection { get; }
        public IMongoCollection<EventItem>? EventsCollection { get; }
        public IMongoCollection<AttendanceRecord>? AttendanceCollection { get; }
        public IMongoCollection<User>? AdminsCollection { get; }
        public IMongoCollection<UserProfile>? ProfilesCollection { get; }
        public bool IsConnected => _isConnected;
        public string ConnectionString => _connectionString;
        public string DatabaseName => _databaseName;
    }
}
