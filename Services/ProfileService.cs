using acm_amtics_website.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace acm_amtics_website.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IMongoDbContext _context;
        private readonly IMemberService _memberService;
        private readonly IEventService _eventService;
        private readonly ILogger<ProfileService> _logger;
        private static readonly List<UserProfile> _fallbackProfiles = new();
        private static readonly object _lock = new();

        public ProfileService(
            IMongoDbContext context,
            IMemberService memberService,
            IEventService eventService,
            ILogger<ProfileService> logger)
        {
            _context = context;
            _memberService = memberService;
            _eventService = eventService;
            _logger = logger;
        }

        public async Task<UserProfile?> GetProfileByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalizedEmail = email.Trim().ToLowerInvariant();

            if (_context.IsConnected && _context.ProfilesCollection != null)
            {
                try
                {
                    var filter = Builders<UserProfile>.Filter.Eq(p => p.Email, normalizedEmail);
                    var profile = await _context.ProfilesCollection.Find(filter).FirstOrDefaultAsync();
                    if (profile != null) return profile;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching UserProfile by email from MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackProfiles.FirstOrDefault(p => p.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<UserProfile?> GetProfileBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            var normalizedSlug = slug.Trim().ToLowerInvariant();

            if (_context.IsConnected && _context.ProfilesCollection != null)
            {
                try
                {
                    var filter = Builders<UserProfile>.Filter.Eq(p => p.Slug, normalizedSlug);
                    var profile = await _context.ProfilesCollection.Find(filter).FirstOrDefaultAsync();
                    if (profile != null) return profile;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching UserProfile by slug from MongoDB");
                }
            }

            lock (_lock)
            {
                var profile = _fallbackProfiles.FirstOrDefault(p => p.Slug.Equals(normalizedSlug, StringComparison.OrdinalIgnoreCase));
                if (profile != null) return profile;
            }

            // If not found directly by slug, try matching email prefix of any registered member
            var members = await _memberService.GetMembersAsync(null, 1, 500);
            var matchedMember = members.Items.FirstOrDefault(m => UserProfile.GenerateSlug(m.Email).Equals(normalizedSlug, StringComparison.OrdinalIgnoreCase));
            if (matchedMember != null)
            {
                return await EnsureProfileCreatedAsync(matchedMember.Email, matchedMember.Name);
            }

            return null;
        }

        public async Task<UserProfile> EnsureProfileCreatedAsync(string email, string? name = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required to create a profile.");

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var existing = await GetProfileByEmailAsync(normalizedEmail);
            if (existing != null)
            {
                return existing;
            }

            // Look up corresponding member record if available
            var membersResult = await _memberService.GetMembersAsync(normalizedEmail, 1, 10);
            var memberMatch = membersResult.Items.FirstOrDefault(m => m.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

            var profileName = !string.IsNullOrWhiteSpace(name) ? name.Trim() : (memberMatch?.Name ?? "ACM Member");
            var slug = UserProfile.GenerateSlug(normalizedEmail);

            var profile = new UserProfile
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Email = normalizedEmail,
                Slug = slug,
                Name = profileName,
                Department = "Computer Science & Engineering",
                AcademicYear = "3rd Year",
                College = "AMTICS, Gandhinagar",
                Bio = "Passionate about design, technology and community building. Excited to learn, collaborate and create meaningful impact with ACM AMTICS.",
                Quote = "Ideas grow when shared.",
                AvatarUrl = memberMatch?.AvatarUrl,
                MemberSince = (memberMatch?.JoinDate ?? DateTime.UtcNow).ToString("MMM yyyy"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (_context.IsConnected && _context.ProfilesCollection != null)
            {
                try
                {
                    await _context.ProfilesCollection.InsertOneAsync(profile);
                    _logger.LogInformation("Automatically created UserProfile for {Email} with slug {Slug}", normalizedEmail, slug);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to insert UserProfile into MongoDB; caching in memory");
                }
            }

            lock (_lock)
            {
                _fallbackProfiles.Add(profile);
            }

            return profile;
        }

        public async Task<UserProfile?> UpdateProfileAsync(string email, EditProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var profile = await EnsureProfileCreatedAsync(normalizedEmail);

            profile.Name = dto.Name.Trim();
            profile.Department = dto.Department.Trim();
            profile.AcademicYear = dto.AcademicYear.Trim();
            profile.Bio = dto.Bio.Trim();
            profile.Quote = dto.Quote.Trim();
            profile.UpdatedAt = DateTime.UtcNow;

            if (_context.IsConnected && _context.ProfilesCollection != null)
            {
                try
                {
                    var filter = Builders<UserProfile>.Filter.Eq(p => p.Email, normalizedEmail);
                    var update = Builders<UserProfile>.Update
                        .Set(p => p.Name, profile.Name)
                        .Set(p => p.Department, profile.Department)
                        .Set(p => p.AcademicYear, profile.AcademicYear)
                        .Set(p => p.Bio, profile.Bio)
                        .Set(p => p.Quote, profile.Quote)
                        .Set(p => p.UpdatedAt, DateTime.UtcNow);

                    await _context.ProfilesCollection.UpdateOneAsync(filter, update);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update UserProfile in MongoDB for {Email}", normalizedEmail);
                }
            }

            lock (_lock)
            {
                var cached = _fallbackProfiles.FirstOrDefault(p => p.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
                if (cached != null)
                {
                    cached.Name = profile.Name;
                    cached.Department = profile.Department;
                    cached.AcademicYear = profile.AcademicYear;
                    cached.Bio = profile.Bio;
                    cached.Quote = profile.Quote;
                    cached.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Also update Member name if member record exists
            var membersResult = await _memberService.GetMembersAsync(normalizedEmail, 1, 10);
            var memberMatch = membersResult.Items.FirstOrDefault(m => m.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
            if (memberMatch != null && memberMatch.Id != null)
            {
                var updateMemberDto = new MemberCreateDto
                {
                    Name = profile.Name,
                    Email = memberMatch.Email,
                    Phone = memberMatch.Phone,
                    CountryCode = memberMatch.CountryCode,
                    EnrollmentNumber = memberMatch.EnrollmentNumber,
                    Role = memberMatch.Role,
                    AssignedEventId = memberMatch.AssignedEventId,
                    AssignedEventName = memberMatch.AssignedEventName,
                    Status = memberMatch.Status
                };
                await _memberService.UpdateMemberAsync(memberMatch.Id, updateMemberDto);
            }

            return profile;
        }

        public async Task<ProfileViewModel> GetProfileViewModelAsync(string email, bool isOwnProfile)
        {
            var profile = await EnsureProfileCreatedAsync(email);
            return await BuildViewModelForProfileAsync(profile, isOwnProfile);
        }

        public async Task<ProfileViewModel?> GetProfileViewModelBySlugAsync(string slug, bool isOwnProfile)
        {
            var profile = await GetProfileBySlugAsync(slug);
            if (profile == null) return null;

            return await BuildViewModelForProfileAsync(profile, isOwnProfile);
        }

        private async Task<ProfileViewModel> BuildViewModelForProfileAsync(UserProfile profile, bool isOwnProfile)
        {
            var vm = new ProfileViewModel
            {
                Profile = profile,
                IsOwnProfile = isOwnProfile
            };

            // Fetch Member record from database
            var membersResult = await _memberService.GetMembersAsync(profile.Email, 1, 10);
            vm.MemberRecord = membersResult.Items.FirstOrDefault(m => m.Email.Equals(profile.Email, StringComparison.OrdinalIgnoreCase));

            // Fetch attended events from MongoDB attendance records
            var attendedEventItems = new List<EventItem>();
            if (_context.IsConnected && _context.AttendanceCollection != null)
            {
                try
                {
                    var attFilter = Builders<AttendanceRecord>.Filter.Eq(a => a.Email, profile.Email);
                    var attRecords = await _context.AttendanceCollection.Find(attFilter).ToListAsync();
                    var eventIds = attRecords.Select(a => a.EventId).Distinct().ToList();

                    foreach (var eid in eventIds)
                    {
                        var eventObj = await _eventService.GetEventByIdAsync(eid);
                        if (eventObj != null)
                        {
                            attendedEventItems.Add(eventObj);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching attended events for user profile {Email}", profile.Email);
                }
            }

            vm.AttendedEvents = attendedEventItems.OrderByDescending(e => e.Date).ToList();

            // Coordinator roles from database
            var roles = new List<MemberCoordinatorRoleDto>();
            if (vm.MemberRecord != null && !string.IsNullOrWhiteSpace(vm.MemberRecord.AssignedEventName))
            {
                roles.Add(new MemberCoordinatorRoleDto
                {
                    EventId = vm.MemberRecord.AssignedEventId ?? "",
                    EventName = vm.MemberRecord.AssignedEventName,
                    RoleTitle = $"{vm.MemberRecord.Role} / Coordinator",
                    Date = vm.MemberRecord.JoinDate.ToString("dd MMM yyyy"),
                    Status = "Current Role",
                    StatusClass = "role-badge-current",
                    Image = "/images/events/web-dev-workshop.jpg"
                });
            }

            vm.CoordinatorRoles = roles;

            // Compute dynamic badges based on database metrics
            vm.Badges = CalculateBadges(vm.AttendedEvents.Count, vm.CoordinatorRoles.Count, 0);

            return vm;
        }

        private List<BadgeDto> CalculateBadges(int eventsCount, int coordinatorRolesCount, int projectsCount)
        {
            var badges = new List<BadgeDto>
            {
                new BadgeDto
                {
                    Id = "first-event",
                    Title = "First Event",
                    Description = "Attended your first event",
                    FullDesc = "Awarded automatically when a member attends their very first ACM AMTICS event.",
                    ColorType = "purple",
                    Grad1 = "#8B5CF6",
                    Grad2 = "#6D28D9",
                    Icon = @"<svg width=""22"" height=""22"" fill=""currentColor"" viewBox=""0 0 24 24""><path d=""M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z""/></svg>",
                    Earned = eventsCount >= 1,
                    ProgressText = eventsCount >= 1 ? "Earned • 1+ Event Attended" : $"{eventsCount} / 1 Event Attended"
                },
                new BadgeDto
                {
                    Id = "first-coordinator",
                    Title = "First Coordinator",
                    Description = "Assigned as event coordinator",
                    FullDesc = "Awarded automatically when assigned as a coordinator for an ACM AMTICS event.",
                    ColorType = "green",
                    Grad1 = "#10B981",
                    Grad2 = "#059669",
                    Icon = @"<svg width=""22"" height=""22"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor""><path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M17 20h5v-2a3 3 0 00-5-3.512M9 20H4v-2a3 3 0 015-3.512M12 11a4 4 0 100-8 4 4 0 000 8zm0 2a7 7 0 00-7 7h14a7 7 0 00-7-7z"" /></svg>",
                    Earned = coordinatorRolesCount >= 1,
                    ProgressText = coordinatorRolesCount >= 1 ? "Earned • Assigned Coordinator" : $"{coordinatorRolesCount} / 1 Coordinator Assignment"
                },
                new BadgeDto
                {
                    Id = "first-project",
                    Title = "First Project",
                    Description = "Reserved for future projects",
                    FullDesc = "Reserved for future project-submission functionality.",
                    ColorType = "orange",
                    Grad1 = "#F59E0B",
                    Grad2 = "#D97706",
                    Icon = @"<svg width=""22"" height=""22"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor""><path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2.5"" d=""M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"" /></svg>",
                    Earned = projectsCount >= 1,
                    ProgressText = projectsCount >= 1 ? "Earned • 1 Project Submitted" : "Reserved for future project submissions"
                },
                new BadgeDto
                {
                    Id = "event-enthusiast",
                    Title = "Event Enthusiast",
                    Description = "Attended 5+ events",
                    FullDesc = "Awarded to members who actively attend 5 or more ACM AMTICS events.",
                    ColorType = "cyan",
                    Grad1 = "#06B6D4",
                    Grad2 = "#0284C7",
                    Icon = @"<svg width=""22"" height=""22"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor""><path stroke-linecap=""round"" stroke-linejoin=""round"" stroke-width=""2"" d=""M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"" /></svg>",
                    Earned = eventsCount >= 5,
                    ProgressText = eventsCount >= 5 ? "Earned • 5+ Events" : $"{eventsCount} / 5 Events Attended"
                }
            };

            return badges;
        }
    }
}
