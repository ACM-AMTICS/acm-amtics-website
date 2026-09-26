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
                ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
                Status = string.IsNullOrWhiteSpace(dto.Status) ? "Upcoming" : dto.Status.Trim(),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                AttendeesCount = 0,
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
                        .Set(e => e.Description, dto.Description?.Trim() ?? string.Empty)
                        .Set(e => e.ImageUrl, string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim())
                        .Set(e => e.Status, string.IsNullOrWhiteSpace(dto.Status) ? "Upcoming" : dto.Status.Trim());

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
            item.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
            item.Status = string.IsNullOrWhiteSpace(dto.Status) ? "Upcoming" : dto.Status.Trim();
            return true;
        }

        public async Task<bool> DeleteEventAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            bool deleted = false;
            if (_context.IsConnected && _context.EventsCollection != null)
            {
                try
                {
                    var filter = Builders<EventItem>.Filter.Eq(e => e.Id, id);
                    var res = await _context.EventsCollection.DeleteOneAsync(filter);
                    if (res.DeletedCount > 0)
                    {
                        deleted = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting event from MongoDB");
                }
            }

            lock (_lock)
            {
                if (DeleteFromFallback(id))
                {
                    deleted = true;
                }
            }

            try
            {
                await _attendanceService.DeleteAttendeesByEventIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup attendees for deleted event {Id}", id);
            }

            return deleted || true;
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
            return new List<EventItem>();
        }
    }
}
