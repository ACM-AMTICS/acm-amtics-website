using acm_amtics_website.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BCrypt.Net;

namespace acm_amtics_website.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<User>? _usersCollection;
        private readonly ILogger<UserService> _logger;

        public UserService(IOptions<MongoDbSettings> settings, IConfiguration configuration, ILogger<UserService> logger)
        {
            _logger = logger;
            try
            {
                // Priority: Env VAR > appsettings MongoDbSettings > Default connection string
                var connStr = Environment.GetEnvironmentVariable("MONGODB_URI") 
                             ?? configuration["MongoDB:ConnectionString"] 
                             ?? settings.Value.ConnectionString;

                var dbName = Environment.GetEnvironmentVariable("MONGODB_DB_NAME") 
                             ?? configuration["MongoDB:DatabaseName"] 
                             ?? settings.Value.DatabaseName;

                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    var client = new MongoClient(connStr);
                    var database = client.GetDatabase(dbName);
                    var collectionName = settings.Value.UsersCollectionName ?? "users";
                    _usersCollection = database.GetCollection<User>(collectionName);
                }
                else
                {
                    _logger.LogWarning("MongoDB Connection string is not configured yet. Set MONGODB_URI environment variable or update appsettings.json.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MongoDB Client.");
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (_usersCollection == null) return null;
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.Email, email.Trim().ToLowerInvariant());
                return await _usersCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by email from MongoDB");
                return null;
            }
        }

        public async Task<bool> ValidatePasswordAsync(string email, string password)
        {
            var user = await GetByEmailAsync(email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password hash");
                return false;
            }
        }

        public async Task<User> CreateUserAsync(string email, string password, string fullName = "ACM Member")
        {
            if (_usersCollection == null)
            {
                throw new InvalidOperationException("MongoDB is not connected. Please provide a valid MongoDB URL in configuration.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow
            };

            await _usersCollection.InsertOneAsync(user);
            return user;
        }

        public async Task UpdateLastLoginAsync(string id)
        {
            if (_usersCollection == null || string.IsNullOrEmpty(id)) return;
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.Id, id);
                var update = Builders<User>.Update.Set(u => u.LastLogin, DateTime.UtcNow);
                await _usersCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user last login timestamp");
            }
        }

        public async Task SeedDefaultAdminAsync()
        {
            if (_usersCollection == null) return;
            try
            {
                var count = await _usersCollection.CountDocumentsAsync(Builders<User>.Filter.Empty);
                if (count == 0)
                {
                    _logger.LogInformation("Seeding default admin user into MongoDB...");
                    await CreateUserAsync("admin@acmamtics.org", "Admin@123456", "ACM Admin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not seed default admin user (MongoDB may not be reachable yet).");
            }
        }
    }
}
