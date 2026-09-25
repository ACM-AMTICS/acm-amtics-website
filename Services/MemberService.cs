using acm_amtics_website.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace acm_amtics_website.Services
{
    public class MemberService : IMemberService
    {
        private readonly IMongoDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<MemberService> _logger;
        private static readonly List<Member> _fallbackMembers = new();
        private static readonly object _lock = new();
        private static bool _seeded = false;

        public MemberService(IMongoDbContext context, IUserService userService, ILogger<MemberService> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
            EnsureDataSeeded();
        }

        private void EnsureDataSeeded()
        {
            lock (_lock)
            {
                if (_seeded) return;

                var initialList = GenerateInitialMembers();
                _fallbackMembers.AddRange(initialList);

                // If Mongo is connected, ensure preserved members exist without deleting custom members
                if (_context.IsConnected && _context.MembersCollection != null)
                {
                    try
                    {
                        var allowedIds = new[] { "65a100000000000000000000", "65a10000000000000000000a" };
                        // Ensure the preserved members exist
                        foreach (var member in initialList.Where(m => allowedIds.Contains(m.Id)))
                        {
                            var filter = Builders<Member>.Filter.Eq(m => m.Id, member.Id);
                            var existing = _context.MembersCollection.Find(filter).FirstOrDefault();
                            if (existing == null)
                            {
                                _context.MembersCollection.InsertOne(member);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup/seed members collection in MongoDB; using fallback cache.");
                    }
                }

                _seeded = true;
            }
        }

        public async Task<PaginatedResult<Member>> GetMembersAsync(string? search, int page = 1, int pageSize = 8)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 8;

            if (_context.IsConnected && _context.MembersCollection != null)
            {
                try
                {
                    FilterDefinition<Member> filter = FilterDefinition<Member>.Empty;
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var s = search.Trim();
                        var nameFilter = Builders<Member>.Filter.Regex(m => m.Name, new BsonRegularExpression(s, "i"));
                        var emailFilter = Builders<Member>.Filter.Regex(m => m.Email, new BsonRegularExpression(s, "i"));
                        var roleFilter = Builders<Member>.Filter.Regex(m => m.Role, new BsonRegularExpression(s, "i"));
                        var enrollFilter = Builders<Member>.Filter.Regex(m => m.EnrollmentNumber, new BsonRegularExpression(s, "i"));
                        filter = Builders<Member>.Filter.Or(nameFilter, emailFilter, roleFilter, enrollFilter);
                    }

                    var totalCount = (int)await _context.MembersCollection.CountDocumentsAsync(filter);
                    var items = await _context.MembersCollection.Find(filter)
                        .Sort(Builders<Member>.Sort.Ascending(m => m.OrderIndex).Descending(m => m.JoinDate))
                        .Skip((page - 1) * pageSize)
                        .Limit(pageSize)
                        .ToListAsync();

                    return new PaginatedResult<Member>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MongoDB read error in GetMembersAsync, falling back to in-memory store.");
                }
            }

            // Fallback in-memory
            lock (_lock)
            {
                IEnumerable<Member> query = _fallbackMembers;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLowerInvariant();
                    query = query.Where(m =>
                        (m.Name != null && m.Name.ToLowerInvariant().Contains(s)) ||
                        (m.Email != null && m.Email.ToLowerInvariant().Contains(s)) ||
                        (m.Role != null && m.Role.ToLowerInvariant().Contains(s)) ||
                        (m.EnrollmentNumber != null && m.EnrollmentNumber.ToLowerInvariant().Contains(s))
                    );
                }

                var list = query.OrderBy(m => m.OrderIndex).ThenByDescending(m => m.JoinDate).ToList();
                var total = list.Count;
                var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return new PaginatedResult<Member>
                {
                    Items = pageItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                };
            }
        }

        public async Task<Member?> GetMemberByIdAsync(string id)
        {
            if (_context.IsConnected && _context.MembersCollection != null)
            {
                try
                {
                    var filter = Builders<Member>.Filter.Eq(m => m.Id, id);
                    return await _context.MembersCollection.Find(filter).FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting member by id from MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackMembers.FirstOrDefault(m => m.Id == id);
            }
        }

        public async Task<Member> CreateMemberAsync(MemberCreateDto dto)
        {
            var member = new Member
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = dto.Name.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                Phone = dto.Phone.Trim(),
                CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "+91" : dto.CountryCode.Trim(),
                EnrollmentNumber = dto.EnrollmentNumber.Trim(),
                Role = dto.Role.Trim(),
                AssignedEventId = dto.AssignedEventId,
                AssignedEventName = dto.AssignedEventName,
                JoinDate = DateTime.UtcNow,
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim(),
                OrderIndex = 0 // Appears at the very top of the table
            };

            if (_context.IsConnected && _context.MembersCollection != null)
            {
                try
                {
                    await _context.MembersCollection.InsertOneAsync(member);
                    _logger.LogInformation("Member {Name} saved to MongoDB collection.", member.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to insert member into MongoDB");
                }
            }

            lock (_lock)
            {
                _fallbackMembers.Insert(0, member);
            }

            if (string.Equals(member.Role, "Coordinator", StringComparison.OrdinalIgnoreCase))
            {
                await _userService.UpsertCoordinatorUserAsync(member.Email, member.Name, member.AssignedEventId, member.AssignedEventName);
            }

            return member;
        }

        public async Task<Member?> UpdateMemberAsync(string id, MemberCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            Member? updatedMember = null;

            if (_context.IsConnected && _context.MembersCollection != null)
            {
                try
                {
                    var filter = Builders<Member>.Filter.Eq(m => m.Id, id);
                    var update = Builders<Member>.Update
                        .Set(m => m.Name, dto.Name.Trim())
                        .Set(m => m.Email, dto.Email.Trim().ToLowerInvariant())
                        .Set(m => m.Phone, dto.Phone.Trim())
                        .Set(m => m.CountryCode, string.IsNullOrWhiteSpace(dto.CountryCode) ? "+91" : dto.CountryCode.Trim())
                        .Set(m => m.EnrollmentNumber, dto.EnrollmentNumber.Trim())
                        .Set(m => m.Role, dto.Role.Trim())
                        .Set(m => m.AssignedEventId, dto.AssignedEventId)
                        .Set(m => m.AssignedEventName, dto.AssignedEventName)
                        .Set(m => m.Status, string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim());

                    updatedMember = await _context.MembersCollection.FindOneAndUpdateAsync(
                        filter,
                        update,
                        new FindOneAndUpdateOptions<Member> { ReturnDocument = ReturnDocument.After }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update member in MongoDB");
                }
            }

            lock (_lock)
            {
                var existing = _fallbackMembers.FirstOrDefault(m => m.Id == id);
                if (existing != null)
                {
                    existing.Name = dto.Name.Trim();
                    existing.Email = dto.Email.Trim().ToLowerInvariant();
                    existing.Phone = dto.Phone.Trim();
                    existing.CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "+91" : dto.CountryCode.Trim();
                    existing.EnrollmentNumber = dto.EnrollmentNumber.Trim();
                    existing.Role = dto.Role.Trim();
                    existing.AssignedEventId = dto.AssignedEventId;
                    existing.AssignedEventName = dto.AssignedEventName;
                    existing.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status.Trim();

                    if (updatedMember == null)
                    {
                        updatedMember = existing;
                    }
                }
            }

            if (updatedMember != null && string.Equals(updatedMember.Role, "Coordinator", StringComparison.OrdinalIgnoreCase))
            {
                await _userService.UpsertCoordinatorUserAsync(updatedMember.Email, updatedMember.Name, updatedMember.AssignedEventId, updatedMember.AssignedEventName);
            }

            return updatedMember;
        }

        public async Task<bool> DeleteMemberAsync(string id)
        {
            var deleted = false;
            if (_context.IsConnected && _context.MembersCollection != null)
            {
                try
                {
                    var result = await _context.MembersCollection.DeleteOneAsync(m => m.Id == id);
                    deleted = result.DeletedCount > 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete member from MongoDB");
                }
            }

            lock (_lock)
            {
                var item = _fallbackMembers.FirstOrDefault(m => m.Id == id);
                if (item != null)
                {
                    _fallbackMembers.Remove(item);
                    deleted = true;
                }
            }

            return deleted;
        }

        public async Task<int> GetTotalMembersCountAsync()
        {
            if (_context.IsConnected && _context.MembersCollection != null)
            {
                try
                {
                    return (int)await _context.MembersCollection.CountDocumentsAsync(FilterDefinition<Member>.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting member count from MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackMembers.Count;
            }
        }

        private static List<Member> GenerateInitialMembers()
        {
            return new List<Member>
            {
                new Member
                {
                    Id = "65a100000000000000000000",
                    Name = "Hetvi Dholiya",
                    Email = "hetvi022gamil.com",
                    Phone = "9876543200",
                    CountryCode = "+91",
                    EnrollmentNumber = "2024031035000",
                    Role = "Coordinator",
                    AssignedEventName = "Web Development Workshop",
                    JoinDate = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                    Status = "Active",
                    OrderIndex = 0
                },
                new Member
                {
                    Id = "65a10000000000000000000a",
                    Name = "Hetvi Dholiya",
                    Email = "hetvidholiya77@gmail.com",
                    Phone = "9876543200",
                    CountryCode = "+91",
                    EnrollmentNumber = "2024031035099",
                    Role = "Admin",
                    JoinDate = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                    Status = "Active",
                    OrderIndex = 0
                }
            };
        }
    }
}
