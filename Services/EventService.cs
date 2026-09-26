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
                        foreach (var eventItem in initialEvents)
                        {
                            var filter = Builders<EventItem>.Filter.Eq(e => e.Id, eventItem.Id);
                            var existing = _context.EventsCollection.Find(filter).FirstOrDefault();
                            if (existing == null)
                            {
                                _context.EventsCollection.InsertOne(eventItem);
                            }
                            else if (existing.Id == "65b000000000000000000001" || existing.Id == "65b000000000000000000004")
                            {
                                var update = Builders<EventItem>.Update
                                    .Set(e => e.Status, eventItem.Status)
                                    .Set(e => e.OrderIndex, eventItem.OrderIndex)
                                    .Set(e => e.Date, eventItem.Date)
                                    .Set(e => e.ImageUrl, eventItem.ImageUrl);
                                _context.EventsCollection.UpdateOne(filter, update);
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

        public async Task<EventsViewModel> GetPublicEventsAsync(string? search, string? type, int? year, string? sortBy, string? tab, int page = 1, int pageSize = 8)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 8;
            tab = string.IsNullOrWhiteSpace(tab) ? "completed" : tab.Trim().ToLowerInvariant();
            sortBy = string.IsNullOrWhiteSpace(sortBy) ? "recent" : sortBy.Trim().ToLowerInvariant();

            List<EventItem> allEvents = new();

            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    allEvents = await _context.EventsCollection.Find(FilterDefinition<EventItem>.Empty).ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load events from MongoDB, falling back to memory store.");
                }
            }

            if (allEvents.Count == 0)
            {
                lock (_lock)
                {
                    allEvents = new List<EventItem>(_fallbackEvents);
                }
            }

            // Sync attendees counts if needed
            foreach (var item in allEvents)
            {
                if (item.AttendeesCount == 0 && !string.IsNullOrEmpty(item.Id))
                {
                    try
                    {
                        var stats = await _attendanceService.GetEventAttendeesStatsAsync(item.Id);
                        if (stats.TotalAttendees > 0)
                        {
                            item.AttendeesCount = stats.TotalAttendees;
                        }
                    }
                    catch { }
                }
            }

            // Calculate overall counts for tabs
            var completedCount = allEvents.Count(e => !string.Equals(e.Status, "Upcoming", StringComparison.OrdinalIgnoreCase));
            var upcomingCount = allEvents.Count(e => string.Equals(e.Status, "Upcoming", StringComparison.OrdinalIgnoreCase));

            // Available distinct categories and years
            var availableTypes = allEvents.Select(e => e.Category).Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c).ToList();
            if (!availableTypes.Contains("Workshop")) availableTypes.Insert(0, "Workshop");
            if (!availableTypes.Contains("Seminar")) availableTypes.Add("Seminar");
            if (!availableTypes.Contains("Competition")) availableTypes.Add("Competition");
            if (!availableTypes.Contains("Talk")) availableTypes.Add("Talk");

            var availableYears = allEvents.Select(e => e.Date.Year).Distinct().OrderByDescending(y => y).ToList();
            if (!availableYears.Contains(2024)) availableYears.Add(2024);
            if (!availableYears.Contains(2025)) availableYears.Add(2025);
            if (!availableYears.Contains(2026)) availableYears.Add(2026);
            availableYears = availableYears.OrderByDescending(y => y).ToList();

            // Filter by active tab
            var filtered = allEvents.AsEnumerable();
            if (tab == "upcoming")
            {
                filtered = filtered.Where(e => string.Equals(e.Status, "Upcoming", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                filtered = filtered.Where(e => !string.Equals(e.Status, "Upcoming", StringComparison.OrdinalIgnoreCase));
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(e =>
                    (e.Name != null && e.Name.ToLowerInvariant().Contains(s)) ||
                    (e.Category != null && e.Category.ToLowerInvariant().Contains(s)) ||
                    (e.Description != null && e.Description.ToLowerInvariant().Contains(s)) ||
                    (e.Venue != null && e.Venue.ToLowerInvariant().Contains(s))
                );
            }

            // Type filter
            if (!string.IsNullOrWhiteSpace(type) && !type.Equals("All Types", StringComparison.OrdinalIgnoreCase) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(e => string.Equals(e.Category, type, StringComparison.OrdinalIgnoreCase));
            }

            // Year filter
            if (year.HasValue && year.Value > 0)
            {
                filtered = filtered.Where(e => e.Date.Year == year.Value);
            }

            // Sorting
            if (sortBy == "oldest")
            {
                filtered = filtered.OrderBy(e => e.Date);
            }
            else
            {
                // Most Recent: order by OrderIndex ascending then Date descending
                filtered = filtered.OrderBy(e => e.OrderIndex > 0 ? e.OrderIndex : 999).ThenByDescending(e => e.Date);
            }

            var matchingList = filtered.ToList();
            var totalCount = matchingList.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pageItems = matchingList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return new EventsViewModel
            {
                Events = pageItems,
                SearchQuery = search,
                SelectedType = type,
                SelectedYear = year,
                SortBy = sortBy,
                ActiveTab = tab,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalCount = totalCount,
                CompletedCount = completedCount,
                UpcomingCount = upcomingCount,
                AvailableTypes = availableTypes,
                AvailableYears = availableYears
            };
        }

        private static List<EventItem> GenerateInitialEvents()
        {
            return new List<EventItem>
            {
                // Page 1: Canonical 8 events from the UI mockup
                new EventItem
                {
                    Id = "65b000000000000000000011",
                    Name = "UI/UX Design Workshop",
                    Date = new DateTime(2024, 12, 14, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 120,
                    Status = "Completed",
                    OrderIndex = 1,
                    ImageUrl = "/images/events/ui-ux-workshop.jpg",
                    Description = "A hands-on workshop on modern UI/UX principles, design tools and real-world projects."
                },
                new EventItem
                {
                    Id = "65b000000000000000000012",
                    Name = "Introduction to AI/ML",
                    Date = new DateTime(2024, 11, 10, 11, 0, 0, DateTimeKind.Utc),
                    Time = "11:00 AM - 01:30 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Seminar",
                    AttendeesCount = 95,
                    Status = "Completed",
                    OrderIndex = 2,
                    ImageUrl = "/images/events/ai-ml-intro.jpg",
                    Description = "An insightful session on the fundamentals of Artificial Intelligence and Machine Learning."
                },
                new EventItem
                {
                    Id = "65b000000000000000000013",
                    Name = "ACM Hackathon 2024",
                    Date = new DateTime(2024, 10, 5, 9, 0, 0, DateTimeKind.Utc),
                    Time = "09:00 AM - 09:00 PM",
                    Venue = "AMTICS Campus",
                    Category = "Competition",
                    AttendeesCount = 180,
                    Status = "Completed",
                    OrderIndex = 3,
                    ImageUrl = "/images/events/acm-hackathon.jpg",
                    Description = "A 24-hour hackathon to solve real-world problems using technology and innovation."
                },
                new EventItem
                {
                    Id = "65b000000000000000000004",
                    Name = "Open Source and GSoC Session",
                    Date = new DateTime(2024, 9, 18, 14, 0, 0, DateTimeKind.Utc),
                    Time = "02:00 PM - 04:30 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Talk",
                    AttendeesCount = 90,
                    Status = "Completed",
                    OrderIndex = 4,
                    ImageUrl = "/images/events/open-source-session.jpg",
                    Description = "A talk on open source, contribution journey and opportunities through Google Summer of Code."
                },
                new EventItem
                {
                    Id = "65b000000000000000000001",
                    Name = "Web Development Workshop",
                    Date = new DateTime(2024, 8, 24, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Lab 3, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 110,
                    Status = "Completed",
                    OrderIndex = 5,
                    ImageUrl = "/images/events/web-dev-workshop.jpg",
                    Description = "A practical workshop covering modern web development technologies and best practices."
                },
                new EventItem
                {
                    Id = "65b000000000000000000014",
                    Name = "Career Guidance Session",
                    Date = new DateTime(2024, 8, 12, 15, 0, 0, DateTimeKind.Utc),
                    Time = "03:00 PM - 05:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Seminar",
                    AttendeesCount = 85,
                    Status = "Completed",
                    OrderIndex = 6,
                    ImageUrl = "/images/events/career-guidance.jpg",
                    Description = "Interact with industry experts and get insights on career opportunities in tech."
                },
                new EventItem
                {
                    Id = "65b000000000000000000015",
                    Name = "Tech for Social Good",
                    Date = new DateTime(2024, 7, 20, 10, 30, 0, DateTimeKind.Utc),
                    Time = "10:30 AM - 01:00 PM",
                    Venue = "Auditorium, AMTICS",
                    Category = "Talk",
                    AttendeesCount = 60,
                    Status = "Completed",
                    OrderIndex = 7,
                    ImageUrl = "/images/events/tech-social-good.jpg",
                    Description = "A panel discussion on how technology can create a positive impact on society."
                },
                new EventItem
                {
                    Id = "65b000000000000000000016",
                    Name = "Flutter Development Workshop",
                    Date = new DateTime(2024, 7, 5, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Lab 2, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 75,
                    Status = "Completed",
                    OrderIndex = 8,
                    ImageUrl = "/images/events/flutter-workshop.jpg",
                    Description = "Build cross-platform mobile apps using Flutter with hands-on projects and mentorship."
                },

                // Page 2: Additional Completed Events
                new EventItem
                {
                    Id = "65b000000000000000000017",
                    Name = "Cloud Computing & AWS Essentials",
                    Date = new DateTime(2024, 6, 18, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 90,
                    Status = "Completed",
                    OrderIndex = 9,
                    ImageUrl = "/images/gallery/moment-1.jpg",
                    Description = "Hands-on session deploying serverless architectures and microservices on AWS cloud."
                },
                new EventItem
                {
                    Id = "65b000000000000000000018",
                    Name = "Cybersecurity Bootcamp",
                    Date = new DateTime(2024, 5, 28, 11, 0, 0, DateTimeKind.Utc),
                    Time = "11:00 AM - 02:00 PM",
                    Venue = "Lab 3, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 105,
                    Status = "Completed",
                    OrderIndex = 10,
                    ImageUrl = "/images/gallery/moment-2.jpg",
                    Description = "Understanding vulnerability assessments, ethical penetration testing, and defensive security."
                },
                new EventItem
                {
                    Id = "65b000000000000000000019",
                    Name = "Competitive Programming Contest 2024",
                    Date = new DateTime(2024, 5, 15, 14, 0, 0, DateTimeKind.Utc),
                    Time = "02:00 PM - 05:00 PM",
                    Venue = "Lab 1 & 2, AMTICS",
                    Category = "Competition",
                    AttendeesCount = 140,
                    Status = "Completed",
                    OrderIndex = 11,
                    ImageUrl = "/images/gallery/moment-3.jpg",
                    Description = "An adrenaline-packed coding clash testing algorithms, dynamic programming, and data structures."
                },
                new EventItem
                {
                    Id = "65b000000000000000000020",
                    Name = "Git & GitHub Hands-on Masterclass",
                    Date = new DateTime(2024, 4, 22, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 12:30 PM",
                    Venue = "Lab 3, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 115,
                    Status = "Completed",
                    OrderIndex = 12,
                    ImageUrl = "/images/gallery/moment-4.jpg",
                    Description = "Mastering version control, collaborative branching workflows, pull requests, and CI actions."
                },
                new EventItem
                {
                    Id = "65b000000000000000000021",
                    Name = "Machine Learning with Python",
                    Date = new DateTime(2024, 4, 10, 11, 0, 0, DateTimeKind.Utc),
                    Time = "11:00 AM - 01:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Seminar",
                    AttendeesCount = 88,
                    Status = "Completed",
                    OrderIndex = 13,
                    ImageUrl = "/images/gallery/moment-5.jpg",
                    Description = "Deep dive into regression, classification models, Scikit-Learn pipelines, and model evaluation."
                },
                new EventItem
                {
                    Id = "65b000000000000000000022",
                    Name = "Research Paper Writing & Publishing",
                    Date = new DateTime(2024, 3, 25, 14, 30, 0, DateTimeKind.Utc),
                    Time = "02:30 PM - 04:30 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Talk",
                    AttendeesCount = 55,
                    Status = "Completed",
                    OrderIndex = 14,
                    ImageUrl = "/images/gallery/moment-6.jpg",
                    Description = "Expert tips on drafting technical research papers, literature reviews, and journal submissions."
                },
                new EventItem
                {
                    Id = "65b000000000000000000023",
                    Name = "Blockchain & Web3 Fundamentals",
                    Date = new DateTime(2024, 3, 12, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 12:00 PM",
                    Venue = "Auditorium, AMTICS",
                    Category = "Seminar",
                    AttendeesCount = 70,
                    Status = "Completed",
                    OrderIndex = 15,
                    ImageUrl = "/images/gallery/moment-7.jpg",
                    Description = "Introduction to decentralized networks, cryptographic smart contracts, and Ethereum architecture."
                },
                new EventItem
                {
                    Id = "65b000000000000000000024",
                    Name = "CodeRelay Sprint Competition",
                    Date = new DateTime(2024, 2, 20, 14, 0, 0, DateTimeKind.Utc),
                    Time = "02:00 PM - 05:00 PM",
                    Venue = "Lab 1, AMTICS",
                    Category = "Competition",
                    AttendeesCount = 130,
                    Status = "Completed",
                    OrderIndex = 16,
                    ImageUrl = "/images/events/acm-hackathon.jpg",
                    Description = "Fast-paced team relay coding where teammates alternate writing code under strict time constraints."
                },

                // Page 3: Completed Events from 2024 & 2023 (making total 24)
                new EventItem
                {
                    Id = "65b000000000000000000025",
                    Name = "Intro to DevOps & Docker",
                    Date = new DateTime(2024, 2, 5, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Lab 3, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 80,
                    Status = "Completed",
                    OrderIndex = 17,
                    ImageUrl = "/images/events/web-dev-workshop.jpg",
                    Description = "Hands-on containerization with Docker, multi-stage builds, and basic Kubernetes cluster concepts."
                },
                new EventItem
                {
                    Id = "65b000000000000000000026",
                    Name = "ACM Hour of Code 2023",
                    Date = new DateTime(2023, 12, 15, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 150,
                    Status = "Completed",
                    OrderIndex = 18,
                    ImageUrl = "/images/events/ui-ux-workshop.jpg",
                    Description = "Global Hour of Code initiative introducing coding fundamentals to junior engineering students."
                },
                new EventItem
                {
                    Id = "65b000000000000000000027",
                    Name = "Android Development Bootcamp",
                    Date = new DateTime(2023, 11, 20, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Lab 2, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 95,
                    Status = "Completed",
                    OrderIndex = 19,
                    ImageUrl = "/images/events/flutter-workshop.jpg",
                    Description = "Native Android app engineering using Kotlin, Jetpack Compose, and architectural components."
                },
                new EventItem
                {
                    Id = "65b000000000000000000028",
                    Name = "Data Science & Analytics Seminar",
                    Date = new DateTime(2023, 10, 10, 11, 0, 0, DateTimeKind.Utc),
                    Time = "11:00 AM - 01:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Seminar",
                    AttendeesCount = 85,
                    Status = "Completed",
                    OrderIndex = 20,
                    ImageUrl = "/images/gallery/moment-4.jpg",
                    Description = "Exploring big data handling, exploratory data analysis with Pandas, and data visualization."
                },
                new EventItem
                {
                    Id = "65b000000000000000000029",
                    Name = "AMTICS CodeStorm Hackathon 2023",
                    Date = new DateTime(2023, 9, 15, 9, 0, 0, DateTimeKind.Utc),
                    Time = "09:00 AM - 09:00 PM",
                    Venue = "AMTICS Campus",
                    Category = "Competition",
                    AttendeesCount = 160,
                    Status = "Completed",
                    OrderIndex = 21,
                    ImageUrl = "/images/events/acm-hackathon.jpg",
                    Description = "Annual flagship coding sprint with real-world problem statements and industry mentorship."
                },
                new EventItem
                {
                    Id = "65b000000000000000000030",
                    Name = "Higher Studies Abroad Guidance",
                    Date = new DateTime(2023, 8, 18, 14, 0, 0, DateTimeKind.Utc),
                    Time = "02:00 PM - 04:00 PM",
                    Venue = "Auditorium, AMTICS",
                    Category = "Talk",
                    AttendeesCount = 110,
                    Status = "Completed",
                    OrderIndex = 22,
                    ImageUrl = "/images/events/career-guidance.jpg",
                    Description = "Alumni interactive panel on GRE, TOEFL preparation, university selection, and scholarships."
                },
                new EventItem
                {
                    Id = "65b000000000000000000031",
                    Name = "Game Development with Unity",
                    Date = new DateTime(2023, 7, 25, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Lab 3, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 70,
                    Status = "Completed",
                    OrderIndex = 23,
                    ImageUrl = "/images/gallery/moment-2.jpg",
                    Description = "Building physics-driven 2D and 3D arcade games using Unity engine and C# scripting."
                },
                new EventItem
                {
                    Id = "65b000000000000000000032",
                    Name = "ACM Induction & Orientation",
                    Date = new DateTime(2023, 7, 10, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 12:00 PM",
                    Venue = "Auditorium, AMTICS",
                    Category = "Talk",
                    AttendeesCount = 200,
                    Status = "Completed",
                    OrderIndex = 24,
                    ImageUrl = "/images/events/open-source-session.jpg",
                    Description = "Welcoming freshmen into the ACM community with inspirational keynotes and project showcases."
                },

                // Upcoming Events for the Upcoming Tab
                new EventItem
                {
                    Id = "65b000000000000000000050",
                    Name = "Next-Gen AI & Agentic Systems",
                    Date = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc),
                    Time = "10:00 AM - 01:00 PM",
                    Venue = "Seminar Hall, AMTICS",
                    Category = "Workshop",
                    AttendeesCount = 0,
                    Status = "Upcoming",
                    OrderIndex = 101,
                    ImageUrl = "/images/events/ai-ml-intro.jpg",
                    Description = "Building multi-agent autonomous software architectures and multimodal LLM tooling."
                },
                new EventItem
                {
                    Id = "65b000000000000000000051",
                    Name = "ACM National Hackathon 2026",
                    Date = new DateTime(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc),
                    Time = "09:00 AM - 09:00 PM",
                    Venue = "AMTICS Campus",
                    Category = "Competition",
                    AttendeesCount = 0,
                    Status = "Upcoming",
                    OrderIndex = 102,
                    ImageUrl = "/images/events/acm-hackathon.jpg",
                    Description = "36-hour inter-college coding marathon celebrating innovation, hardware hacking, and web3."
                },
                new EventItem
                {
                    Id = "65b000000000000000000052",
                    Name = "Quantum Computing & Cryptography",
                    Date = new DateTime(2026, 11, 28, 14, 0, 0, DateTimeKind.Utc),
                    Time = "02:00 PM - 04:30 PM",
                    Venue = "Auditorium, AMTICS",
                    Category = "Talk",
                    AttendeesCount = 0,
                    Status = "Upcoming",
                    OrderIndex = 103,
                    ImageUrl = "/images/gallery/moment-7.jpg",
                    Description = "Distinguished guest lecture on quantum algorithms, qubits, and post-quantum encryption standards."
                }
            };
        }
    }
}
