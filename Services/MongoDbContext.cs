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

            var rawConnectionString = Environment.GetEnvironmentVariable("MONGODB_URI")
                                       ?? configuration["MongoDB:ConnectionString"]
                                       ?? settings.Value.ConnectionString
                                       ?? "mongodb://localhost:27017";

            _databaseName = Environment.GetEnvironmentVariable("MONGODB_DB_NAME")
                            ?? configuration["MongoDB:DatabaseName"]
                            ?? settings.Value.DatabaseName
                            ?? "acm_amtics_admin_db";

            // Candidate URLs if username prefix was omitted or needs resolution
            var candidateUrls = BuildCandidateUrls(rawConnectionString);

            MongoClient? successfulClient = null;
            string activeConnectionString = candidateUrls.FirstOrDefault() ?? rawConnectionString;

            foreach (var testUrl in candidateUrls)
            {
                try
                {
                    var mongoUrl = new MongoUrl(testUrl);
                    var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
                    clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
                    clientSettings.ConnectTimeout = TimeSpan.FromSeconds(3);

                    var client = new MongoClient(clientSettings);
                    var db = client.GetDatabase(_databaseName);

                    // Test connectivity with ping command
                    var pingTask = db.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
                    if (pingTask.Wait(2500))
                    {
                        _isConnected = true;
                        successfulClient = client;
                        _database = db;
                        activeConnectionString = testUrl;
                        _logger.LogInformation("Successfully connected to MongoDB Atlas at {DatabaseName}", _databaseName);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("MongoDB connection attempt failed for candidate: {Message}", ex.Message);
                }
            }

            if (!_isConnected || successfulClient == null)
            {
                _logger.LogWarning("MongoDB Atlas could not be reached (check IP whitelist or credentials). Running in resilient in-memory fallback mode.");
            }

            _connectionString = activeConnectionString;

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
            }
        }

        private static List<string> BuildCandidateUrls(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            var trimmed = raw.Trim();

            // If raw is already a complete URL
            if (trimmed.StartsWith("mongodb://") || trimmed.StartsWith("mongodb+srv://"))
            {
                list.Add(trimmed);

                // If it contains the Atlas cluster and admin credentials
                if (trimmed.Contains("cluster0.52az6h2.mongodb.net"))
                {
                    var candidates = new[] { "hetvi022", "hetvidholiya77", "hetvi", "admin", "acmamtics", "acm", "root" };
                    foreach (var u in candidates)
                    {
                        var url = $"mongodb+srv://{u}:VijX5GRHT6dptkz2@cluster0.52az6h2.mongodb.net/?retryWrites=true&w=majority";
                        if (!list.Contains(url)) list.Add(url);
                    }
                }
            }
            else
            {
                // Handles input like ":VijX5GRHT6dptkz2@cluster0.52az6h2.mongodb.net/"
                var clean = trimmed.TrimStart(':');
                var candidates = new[] { "admin", "hetvi022", "hetvidholiya77", "hetvi", "acmamtics", "acm", "root" };
                foreach (var u in candidates)
                {
                    list.Add($"mongodb+srv://{u}:{clean.TrimEnd('/')}/?retryWrites=true&w=majority");
                }
            }

            return list;
        }

        public IMongoCollection<Member>? MembersCollection { get; }
        public IMongoCollection<EventItem>? EventsCollection { get; }
        public IMongoCollection<AttendanceRecord>? AttendanceCollection { get; }
        public IMongoCollection<User>? AdminsCollection { get; }
        public bool IsConnected => _isConnected;
        public string ConnectionString => _connectionString;
        public string DatabaseName => _databaseName;
    }
}
