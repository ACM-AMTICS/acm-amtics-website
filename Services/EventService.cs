using acm_amtics_website.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace acm_amtics_website.Services
{
    public class EventService : IEventService
    {
        private readonly IMongoDbContext _context;
        private readonly ILogger<EventService> _logger;
        private static readonly List<EventItem> _fallbackEvents = new();
        private static readonly object _lock = new();
        private static bool _seeded = false;

        public EventService(IMongoDbContext context, ILogger<EventService> logger)
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

                var initialEvents = GenerateInitialEvents();
                _fallbackEvents.AddRange(initialEvents);

                if (_context.IsConnected && _context.EventsCollection != null)
                {
                    try
                    {
                        var count = _context.EventsCollection.CountDocuments(FilterDefinition<EventItem>.Empty);
                        if (count == 0)
                        {
                            _logger.LogInformation("Seeding {Count} events into MongoDB...", initialEvents.Count);
                            _context.EventsCollection.InsertMany(initialEvents);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to seed events collection in MongoDB; using fallback memory store.");
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

            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    var filter = Builders<EventItem>.Filter.Eq(e => e.Id, id);
                    var item = await _context.EventsCollection.Find(filter).FirstOrDefaultAsync();
                    if (item != null) return item;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching event by id from MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackEvents.FirstOrDefault(e => e.Id == id);
            }
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
            var totalAttendees = 860;

            lock (_lock)
            {
                var sum = _fallbackEvents.Sum(e => e.AttendeesCount);
                if (sum > 0) totalAttendees = sum;
            }

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
                    Id = "65b000000000000000000002",
                    Name = "Introduction to AI/ML",
                    Date = today.AddDays(1),
                    Time = "11:00 AM - 1:30 PM",
                    Venue = "Lab 4, AMTICS",
                    Category = "Seminar",
                    AttendeesCount = 95,
                    Status = "Upcoming",
                    OrderIndex = 2,
                    Description = "Foundational insights into machine learning algorithms and real-world neural network pipelines."
                },
                new EventItem
                {
                    Id = "65b000000000000000000003",
                    Name = "ACM Hackathon 2024",
                    Date = today.AddDays(16),
                    Time = "09:00 AM - 09:00 PM",
                    Venue = "AMTICS Auditorium & Labs",
                    Category = "Competition",
                    AttendeesCount = 180,
                    Status = "Upcoming",
                    OrderIndex = 3,
                    Description = "Annual 36-hour flagship hackathon of ACM AMTICS with mentorship and sponsor prizes."
                },
                new EventItem
                {
                    Id = "65b000000000000000000004",
                    Name = "Open Source and GSoC Session",
                    Date = today.AddDays(24),
                    Time = "02:00 PM - 04:30 PM",
                    Venue = "Seminar Hall B",
                    Category = "Talk",
                    AttendeesCount = 90,
                    Status = "Upcoming",
                    OrderIndex = 4,
                    Description = "Getting started with open source contributions, git workflows, and GSoC proposal writing."
                },
                new EventItem
                {
                    Id = "65b000000000000000000005",
                    Name = "Flutter Development Workshop",
                    Date = today.AddDays(42),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Lab 2, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 110,
                    Status = "Upcoming",
                    OrderIndex = 5,
                    Description = "Cross-platform mobile application development with Flutter, Dart, and Firebase backend."
                },
                new EventItem
                {
                    Id = "65b000000000000000000006",
                    Name = "Career Guidance Session",
                    Date = today.AddDays(57),
                    Time = "03:00 PM - 05:00 PM",
                    Venue = "Main Auditorium",
                    Category = "Seminar",
                    AttendeesCount = 85,
                    Status = "Upcoming",
                    OrderIndex = 6,
                    Description = "Navigating placement interviews, open-source portfolio development, and resume building."
                },
                new EventItem
                {
                    Id = "65b000000000000000000007",
                    Name = "UI/UX Design Workshop",
                    Date = today.AddDays(81),
                    Time = "10:30 AM - 01:30 PM",
                    Venue = "Design Studio Lab",
                    Category = "Workshop",
                    AttendeesCount = 75,
                    Status = "Upcoming",
                    OrderIndex = 7,
                    Description = "Interactive UI/UX design systems, Figma component auto-layouts, and user usability testing."
                },
                new EventItem
                {
                    Id = "65b000000000000000000008",
                    Name = "Tech for Social Good",
                    Date = today.AddDays(100),
                    Time = "02:00 PM - 04:00 PM",
                    Venue = "Seminar Hall A",
                    Category = "Talk",
                    AttendeesCount = 60,
                    Status = "Upcoming",
                    OrderIndex = 8,
                    Description = "Leveraging open tech, accessible computing, and ethical AI to drive meaningful societal impact."
                },
                // Additional events to reach 18 events total
                new EventItem
                {
                    Id = "65b000000000000000000009",
                    Name = "Cloud Architecture & DevOps",
                    Date = today.AddDays(115),
                    Time = "11:00 AM - 01:30 PM",
                    Venue = "Lab 3, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 20,
                    Status = "Upcoming",
                    OrderIndex = 9
                },
                new EventItem
                {
                    Id = "65b000000000000000000010",
                    Name = "Competitive Programming Round 1",
                    Date = today.AddDays(128),
                    Time = "04:00 PM - 06:00 PM",
                    Venue = "Online Portal",
                    Category = "Competition",
                    AttendeesCount = 15,
                    Status = "Upcoming",
                    OrderIndex = 10
                },
                new EventItem
                {
                    Id = "65b000000000000000000011",
                    Name = "Cybersecurity & Ethical Hacking",
                    Date = today.AddDays(140),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Seminar Hall B",
                    Category = "Seminar",
                    AttendeesCount = 10,
                    Status = "Upcoming",
                    OrderIndex = 11
                },
                new EventItem
                {
                    Id = "65b000000000000000000012",
                    Name = "Data Science with Python",
                    Date = today.AddDays(155),
                    Time = "02:00 PM - 05:00 PM",
                    Venue = "Lab 1, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 10,
                    Status = "Upcoming",
                    OrderIndex = 12
                },
                new EventItem
                {
                    Id = "65b000000000000000000013",
                    Name = "Web3 & Blockchain Deep Dive",
                    Date = today.AddDays(170),
                    Time = "03:00 PM - 05:00 PM",
                    Venue = "Seminar Hall A",
                    Category = "Talk",
                    AttendeesCount = 8,
                    Status = "Upcoming",
                    OrderIndex = 13
                },
                new EventItem
                {
                    Id = "65b000000000000000000014",
                    Name = "ACM AMTICS Annual General Meeting",
                    Date = today.AddDays(185),
                    Time = "04:00 PM - 06:00 PM",
                    Venue = "Auditorium",
                    Category = "Seminar",
                    AttendeesCount = 5,
                    Status = "Upcoming",
                    OrderIndex = 14
                },
                new EventItem
                {
                    Id = "65b000000000000000000015",
                    Name = "Research Paper Writing Workshop",
                    Date = today.AddDays(200),
                    Time = "10:00 AM - 12:30 PM",
                    Venue = "Conference Room",
                    Category = "Workshop",
                    AttendeesCount = 5,
                    Status = "Upcoming",
                    OrderIndex = 15
                },
                new EventItem
                {
                    Id = "65b000000000000000000016",
                    Name = "Git & GitHub Crash Course",
                    Date = today.AddDays(215),
                    Time = "02:00 PM - 04:00 PM",
                    Venue = "Lab 4, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 5,
                    Status = "Upcoming",
                    OrderIndex = 16
                },
                new EventItem
                {
                    Id = "65b000000000000000000017",
                    Name = "Linux & Shell Scripting 101",
                    Date = today.AddDays(230),
                    Time = "11:00 AM - 01:00 PM",
                    Venue = "Lab 2, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 4,
                    Status = "Upcoming",
                    OrderIndex = 17
                },
                new EventItem
                {
                    Id = "65b000000000000000000018",
                    Name = "ACM Student Chapter Orientation",
                    Date = today.AddDays(245),
                    Time = "10:00 AM - 12:00 PM",
                    Venue = "Auditorium",
                    Category = "Talk",
                    AttendeesCount = 3,
                    Status = "Upcoming",
                    OrderIndex = 18
                }
            };
        }
    }
}
