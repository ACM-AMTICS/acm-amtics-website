using acm_amtics_website.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using BCrypt.Net;

namespace acm_amtics_website.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoDbContext _context;
        private readonly ILogger<UserService> _logger;
        private static readonly List<User> _fallbackUsers = new();
        private static readonly object _lock = new();
        private static bool _seeded = false;

        public UserService(IMongoDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
            EnsureDataSeeded();
        }

        private void EnsureDataSeeded()
        {
            lock (_lock)
            {
                if (_seeded) return;

                _fallbackUsers.Clear();

                // 1. Seed requested Admin account: 24amtics131@gmail.com / 87654321
                _fallbackUsers.Add(new User
                {
                    Id = "admin_auth_01",
                    Email = "24amtics131@gmail.com",
                    FullName = "Admin",
                    Role = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("87654321"),
                    CreatedAt = DateTime.UtcNow
                });

                // 2. Seed requested Coordinator Member account: hetvi022gamil.com / hetvi
                _fallbackUsers.Add(new User
                {
                    Id = "coord_hetvi_02",
                    Email = "hetvi022gamil.com",
                    FullName = "Hetvi Dholiya",
                    Role = "Coordinator",
                    AssignedEventName = "Web Development Workshop",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("hetvi"),
                    CreatedAt = DateTime.UtcNow
                });

                _fallbackUsers.Add(new User
                {
                    Id = "coord_hetvi_03",
                    Email = "hetvi022@gmail.com",
                    FullName = "Hetvi Dholiya",
                    Role = "Coordinator",
                    AssignedEventName = "Web Development Workshop",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("hetvi"),
                    CreatedAt = DateTime.UtcNow
                });

                // 3. Coordinator account matching design screenshots (Yahiya)
                _fallbackUsers.Add(new User
                {
                    Id = "coord_yahiya_01",
                    Email = "yahiya@amtics.acm.org",
                    FullName = "Yahiya",
                    Role = "Coordinator",
                    AssignedEventName = "Web Development Workshop",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("hetvi"),
                    CreatedAt = DateTime.UtcNow
                });

                // If MongoDB is reachable, ensure admin collection is updated
                if (_context.IsConnected && _context.AdminsCollection != null)
                {
                    try
                    {
                        SeedMongoAdminsAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to seed admin users into MongoDB; using fallback in-memory store.");
                    }
                }

                _seeded = true;
            }
        }

        private async Task SeedMongoAdminsAsync()
        {
            if (_context.AdminsCollection == null) return;

            foreach (var fallbackUser in _fallbackUsers)
            {
                try
                {
                    var filter = Builders<User>.Filter.Eq(u => u.Email, fallbackUser.Email.ToLowerInvariant());
                    var existing = await _context.AdminsCollection.Find(filter).FirstOrDefaultAsync();
                    if (existing == null)
                    {
                        var userToInsert = new User
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Email = fallbackUser.Email.ToLowerInvariant(),
                            FullName = fallbackUser.FullName,
                            Role = fallbackUser.Role,
                            AssignedEventName = fallbackUser.AssignedEventName,
                            PasswordHash = fallbackUser.PasswordHash,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.AdminsCollection.InsertOneAsync(userToInsert);
                        _logger.LogInformation("Seeded admin account into MongoDB: {Email} ({Role})", fallbackUser.Email, fallbackUser.Role);
                    }
                    else
                    {
                        // Ensure password, role and event are updated to latest
                        var update = Builders<User>.Update
                            .Set(u => u.Role, fallbackUser.Role)
                            .Set(u => u.PasswordHash, fallbackUser.PasswordHash)
                            .Set(u => u.FullName, fallbackUser.FullName)
                            .Set(u => u.AssignedEventName, fallbackUser.AssignedEventName);
                        await _context.AdminsCollection.UpdateOneAsync(filter, update);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not seed user {Email} into MongoDB", fallbackUser.Email);
                }
            }
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Check MongoDB if connected
            if (_context.IsConnected && _context.AdminsCollection != null)
            {
                try
                {
                    var filter = Builders<User>.Filter.Eq(u => u.Email, normalizedEmail);
                    var user = await _context.AdminsCollection.Find(filter).FirstOrDefaultAsync();
                    if (user != null) return user;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching user by email from MongoDB");
                }
            }

            // Fallback store
            lock (_lock)
            {
                return _fallbackUsers.FirstOrDefault(u => u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<bool> ValidatePasswordAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

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
                _logger.LogError(ex, "Error verifying password hash for {Email}", email);
                return false;
            }
        }

        public async Task<User> CreateUserAsync(string email, string password, string fullName = "ACM Member")
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Email = email.Trim().ToLowerInvariant(),
                PasswordHash = passwordHash,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow
            };

            if (_context.IsConnected && _context.AdminsCollection != null)
            {
                try
                {
                    await _context.AdminsCollection.InsertOneAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to insert user into MongoDB; storing in fallback cache");
                }
            }

            lock (_lock)
            {
                _fallbackUsers.Add(user);
            }

            return user;
        }

        public async Task UpsertCoordinatorUserAsync(string email, string fullName, string? assignedEventId, string? assignedEventName)
        {
            if (string.IsNullOrWhiteSpace(email)) return;
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Extract temporary password as email prefix (part before '@')
            var tempPassword = normalizedEmail.Split('@')[0];
            if (string.IsNullOrWhiteSpace(tempPassword))
            {
                tempPassword = "password";
            }
            var tempPasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            if (_context.IsConnected && _context.AdminsCollection != null)
            {
                try
                {
                    var filter = Builders<User>.Filter.Eq(u => u.Email, normalizedEmail);
                    var existing = await _context.AdminsCollection.Find(filter).FirstOrDefaultAsync();
                    if (existing != null)
                    {
                        var update = Builders<User>.Update
                            .Set(u => u.FullName, fullName)
                            .Set(u => u.Role, "Coordinator")
                            .Set(u => u.AssignedEventId, assignedEventId)
                            .Set(u => u.AssignedEventName, assignedEventName);
                        await _context.AdminsCollection.UpdateOneAsync(filter, update);
                    }
                    else
                    {
                        var userToInsert = new User
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            Email = normalizedEmail,
                            FullName = fullName,
                            Role = "Coordinator",
                            AssignedEventId = assignedEventId,
                            AssignedEventName = assignedEventName,
                            PasswordHash = tempPasswordHash,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.AdminsCollection.InsertOneAsync(userToInsert);
                        _logger.LogInformation("Inserted new coordinator user {Email} with temporary password derived from username.", normalizedEmail);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error upserting coordinator user in MongoDB for {Email}", email);
                }
            }

            lock (_lock)
            {
                var existingInMem = _fallbackUsers.FirstOrDefault(u => u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
                if (existingInMem != null)
                {
                    existingInMem.FullName = fullName;
                    existingInMem.Role = "Coordinator";
                    existingInMem.AssignedEventId = assignedEventId;
                    existingInMem.AssignedEventName = assignedEventName;
                }
                else
                {
                    _fallbackUsers.Add(new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = normalizedEmail,
                        FullName = fullName,
                        Role = "Coordinator",
                        AssignedEventId = assignedEventId,
                        AssignedEventName = assignedEventName,
                        PasswordHash = tempPasswordHash,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        public async Task UpdateLastLoginAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (_context.IsConnected && _context.AdminsCollection != null)
            {
                try
                {
                    var filter = Builders<User>.Filter.Eq(u => u.Id, id);
                    var update = Builders<User>.Update.Set(u => u.LastLogin, DateTime.UtcNow);
                    await _context.AdminsCollection.UpdateOneAsync(filter, update);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user last login timestamp in MongoDB");
                }
            }

            lock (_lock)
            {
                var user = _fallbackUsers.FirstOrDefault(u => u.Id == id);
                if (user != null)
                {
                    user.LastLogin = DateTime.UtcNow;
                }
            }
        }

        public async Task SeedDefaultAdminAsync()
        {
            EnsureDataSeeded();
            if (_context.IsConnected && _context.AdminsCollection != null)
            {
                await SeedMongoAdminsAsync();
            }
        }
    }
}
