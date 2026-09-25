using acm_amtics_website.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace acm_amtics_website.Services
{
    public class EventService : IEventService
    {
        private readonly IMongoDbContext _context;
        private readonly IAttendanceService _attendanceService;
        private readonly ILogger<EventService> _logger;
        private static readonly List<EventItem> _fallbackEvents = new();
        private static readonly object _lock = new();
        private static bool _seeded = false;

        public EventService(IMongoDbContext context, IAttendanceService attendanceService, ILogger<EventService> logger)
        {
            _context = context;
            _attendanceService = attendanceService;
            _logger = logger;
            EnsureDataSeeded();
        }

        private void EnsureDataSeeded()
        {
            lock (_lock)
            {
                if (_seeded) return;

                var initialEvents = GenerateInitialEvents();
                _fallbackEvents.AddRange(initialEvents);

                if (_context.IsConnected && _context.EventsCollection != null)
                {
                    try
                    {
                        var allowedIds = new[] { "65b000000000000000000001", "65b000000000000000000004" };
                        foreach (var eventItem in initialEvents.Where(e => allowedIds.Contains(e.Id)))
                        {
                            var filter = Builders<EventItem>.Filter.Eq(e => e.Id, eventItem.Id);
                            var existing = _context.EventsCollection.Find(filter).FirstOrDefault();
                            if (existing == null)
                            {
                                _context.EventsCollection.InsertOne(eventItem);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup/seed events collection in MongoDB; using fallback memory store.");
                    }
                }

                _seeded = true;
            }
        }

        public async Task<PaginatedResult<EventItem>> GetEventsAsync(string? search, int page = 1, int pageSize = 8)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 8;

            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    var filterBuilder = Builders<EventItem>.Filter;
                    var filter = filterBuilder.Empty;

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var regex = new BsonRegularExpression(search.Trim(), "i");
                        filter = filterBuilder.Or(
                            filterBuilder.Regex(e => e.Name, regex),
                            filterBuilder.Regex(e => e.Category, regex),
                            filterBuilder.Regex(e => e.Venue, regex)
                        );
                    }

                    var total = await _context.EventsCollection.CountDocumentsAsync(filter);
                    var items = await _context.EventsCollection.Find(filter)
                        .SortBy(e => e.OrderIndex)
                        .ThenBy(e => e.Date)
                        .Skip((page - 1) * pageSize)
                        .Limit(pageSize)
                        .ToListAsync();

                    foreach (var item in items)
                    {
                        if (!string.IsNullOrEmpty(item.Id))
                        {
                            var stats = await _attendanceService.GetEventAttendeesStatsAsync(item.Id);
                            item.AttendeesCount = stats.TotalAttendees;
                        }
                    }

                    return new PaginatedResult<EventItem>
                    {
                        Items = items,
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = (int)total
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching events from MongoDB; falling back to memory store");
                }
            }

            lock (_lock)
            {
                var query = _fallbackEvents.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLowerInvariant();
                    query = query.Where(e =>
                        (e.Name != null && e.Name.ToLowerInvariant().Contains(s)) ||
                        (e.Category != null && e.Category.ToLowerInvariant().Contains(s)) ||
                        (e.Venue != null && e.Venue.ToLowerInvariant().Contains(s))
                    );
                }

                var list = query.OrderBy(e => e.OrderIndex).ThenBy(e => e.Date).ToList();
                var total = list.Count;
                var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                return new PaginatedResult<EventItem>
                {
                    Items = pageItems,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                };
            }
        }

        public async Task<EventItem?> GetEventByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            EventItem? item = null;
            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    var filter = Builders<EventItem>.Filter.Eq(e => e.Id, id);
                    item = await _context.EventsCollection.Find(filter).FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching event by id from MongoDB");
                }
            }

            if (item == null)
            {
                lock (_lock)
                {
                    item = _fallbackEvents.FirstOrDefault(e => e.Id == id);
                }
            }

            if (item != null && !string.IsNullOrEmpty(item.Id))
            {
                var stats = await _attendanceService.GetEventAttendeesStatsAsync(item.Id);
                item.AttendeesCount = stats.TotalAttendees;
            }

            return item;
        }

        public async Task<EventItem> CreateEventAsync(EventCreateDto dto, string createdBy = "Admin")
        {
            var eventItem = new EventItem
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = dto.Name.Trim(),
                Date = dto.Date,
                Time = string.IsNullOrWhiteSpace(dto.Time) ? "10:00 AM - 1:00 PM" : dto.Time.Trim(),
                Venue = string.IsNullOrWhiteSpace(dto.Venue) ? "Seminar Hall, AMTICS" : dto.Venue.Trim(),
                Category = string.IsNullOrWhiteSpace(dto.Category) ? "Workshop" : dto.Category.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                AttendeesCount = 0,
                Status = "Upcoming",
                OrderIndex = 0 // Appear at top
            };

            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    await _context.EventsCollection.InsertOneAsync(eventItem);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving new event to MongoDB");
                }
            }

            lock (_lock)
            {
                _fallbackEvents.Insert(0, eventItem);
            }

            return eventItem;
        }

        public async Task<bool> UpdateEventAsync(string id, EventCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    var filter = Builders<EventItem>.Filter.Eq(e => e.Id, id);
                    var update = Builders<EventItem>.Update
                        .Set(e => e.Name, dto.Name.Trim())
                        .Set(e => e.Date, dto.Date)
                        .Set(e => e.Time, dto.Time.Trim())
                        .Set(e => e.Venue, dto.Venue.Trim())
                        .Set(e => e.Category, dto.Category.Trim())
                        .Set(e => e.Description, dto.Description?.Trim() ?? string.Empty);

                    var res = await _context.EventsCollection.UpdateOneAsync(filter, update);
                    if (res.ModifiedCount > 0)
                    {
                        UpdateFallback(id, dto);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating event in MongoDB");
                }
            }

            lock (_lock)
            {
                return UpdateFallback(id, dto);
            }
        }

        private bool UpdateFallback(string id, EventCreateDto dto)
        {
            var item = _fallbackEvents.FirstOrDefault(e => e.Id == id);
            if (item == null) return false;

            item.Name = dto.Name.Trim();
            item.Date = dto.Date;
            item.Time = dto.Time.Trim();
            item.Venue = dto.Venue.Trim();
            item.Category = dto.Category.Trim();
            item.Description = dto.Description?.Trim() ?? string.Empty;
            return true;
        }

        public async Task<bool> DeleteEventAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    var filter = Builders<EventItem>.Filter.Eq(e => e.Id, id);
                    var res = await _context.EventsCollection.DeleteOneAsync(filter);
                    if (res.DeletedCount > 0)
                    {
                        DeleteFromFallback(id);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting event from MongoDB");
                }
            }

            lock (_lock)
            {
                return DeleteFromFallback(id);
            }
        }

        private bool DeleteFromFallback(string id)
        {
            var item = _fallbackEvents.FirstOrDefault(e => e.Id == id);
            if (item == null) return false;
            return _fallbackEvents.Remove(item);
        }

        public async Task<EventsStatsDto> GetEventsStatsAsync()
        {
            var totalEvents = await GetTotalEventsCountAsync();
            var totalAttendees = await _attendanceService.GetTotalAttendeesCountAsync();

            return new EventsStatsDto
            {
                TotalEvents = totalEvents,
                EventsChangePercentage = "+20%",
                EventsChangeText = "from last semester",
                TotalAttendees = totalAttendees,
                AttendeesChangePercentage = "+16%",
                AttendeesChangeText = "from last semester"
            };
        }

        public async Task<List<EventItem>> GetActiveEventsAsync()
        {
            var res = await GetEventsAsync(null, 1, 50);
            return res.Items;
        }

        public async Task<int> GetTotalEventsCountAsync()
        {
            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    var count = await _context.EventsCollection.CountDocumentsAsync(FilterDefinition<EventItem>.Empty);
                    return (int)count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error counting events in MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackEvents.Count;
            }
        }

        private static List<EventItem> GenerateInitialEvents()
        {
            var today = DateTime.UtcNow.Date;

            return new List<EventItem>
            {
                new EventItem
                {
                    Id = "65b000000000000000000001",
                    Name = "Web Development Workshop",
                    Date = today, // Shows "Today" badge!
                    Time = "10:00 AM - 1:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 120,
                    Status = "Active",
                    OrderIndex = 1,
                    Description = "Hands-on full stack development workshop with modern web tech and live deployment."
                },
                new EventItem
                {
                    Id = "65b000000000000000000004",
                    Name = "Open Source and GSoC Session",
                    Date = new DateTime(2026, 10, 19, 0, 0, 0, DateTimeKind.Utc),
                    Time = "02:00 PM - 04:30 PM",
                    Venue = "Seminar Hall B",
                    Category = "Talk",
                    AttendeesCount = 90,
                    Status = "Upcoming",
                    OrderIndex = 4,
                    Description = "Getting started with open source contributions, git workflows, and GSoC proposal writing."
                }
            };
        }
    }
}
