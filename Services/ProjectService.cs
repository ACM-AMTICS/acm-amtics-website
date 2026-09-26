using acm_amtics_website.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace acm_amtics_website.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IMongoDbContext _context;
        private readonly ILogger<ProjectService> _logger;
        private static readonly List<ProjectItem> _fallbackProjects = new();
        private static readonly object _lock = new();
        private static bool _seeded = false;

        public ProjectService(IMongoDbContext context, ILogger<ProjectService> logger)
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

                var initialList = GenerateInitialProjects();
                _fallbackProjects.AddRange(initialList);

                if (_context.IsConnected && _context.ProjectsCollection != null)
                {
                    try
                    {
                        var count = _context.ProjectsCollection.CountDocuments(FilterDefinition<ProjectItem>.Empty);
                        if (count == 0)
                        {
                            _context.ProjectsCollection.InsertMany(initialList);
                            _logger.LogInformation("Seeded {Count} projects into MongoDB Projects collection.", initialList.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to seed projects into MongoDB; using fallback store.");
                    }
                }

                _seeded = true;
            }
        }

        public async Task<ProjectsViewModel> GetPublicProjectsAsync(
            string? search = null,
            string? category = null,
            string? technology = null,
            int? year = null,
            string? sortBy = "recent",
            int page = 1,
            int pageSize = 8)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 8;

            var viewModel = new ProjectsViewModel
            {
                SearchQuery = search?.Trim(),
                SelectedCategory = category?.Trim(),
                SelectedTechnology = technology?.Trim(),
                SelectedYear = year,
                SortBy = string.IsNullOrWhiteSpace(sortBy) ? "recent" : sortBy.Trim().ToLowerInvariant(),
                CurrentPage = page,
                PageSize = pageSize
            };

            // Retrieve all projects from MongoDB or fallback to extract available filter options
            List<ProjectItem> allSourceProjects = new();

            if (_context.IsConnected && _context.ProjectsCollection != null)
            {
                try
                {
                    allSourceProjects = await _context.ProjectsCollection.Find(FilterDefinition<ProjectItem>.Empty).ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load projects from MongoDB; falling back to in-memory dataset.");
                    lock (_lock) { allSourceProjects = _fallbackProjects.ToList(); }
                }
            }
            else
            {
                lock (_lock) { allSourceProjects = _fallbackProjects.ToList(); }
            }

            // Extract dynamic filter lists from actual data
            viewModel.AvailableCategories = allSourceProjects
                .Select(p => p.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            viewModel.AvailableTechnologies = allSourceProjects
                .SelectMany(p => p.Technologies ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t)
                .ToList();

            viewModel.AvailableYears = allSourceProjects
                .Select(p => p.Year)
                .Where(y => y > 2000)
                .Distinct()
                .OrderByDescending(y => y)
                .ToList();

            // Apply Filters
            var filtered = allSourceProjects.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(viewModel.SearchQuery))
            {
                var q = viewModel.SearchQuery;
                filtered = filtered.Where(p =>
                    (p.Title != null && p.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.ShortDescription != null && p.ShortDescription.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.FullDescription != null && p.FullDescription.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Category != null && p.Category.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.SubCategory != null && p.SubCategory.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.TeamName != null && p.TeamName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (p.Technologies != null && p.Technologies.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)))
                );
            }

            if (!string.IsNullOrWhiteSpace(viewModel.SelectedCategory))
            {
                filtered = filtered.Where(p => string.Equals(p.Category, viewModel.SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(viewModel.SelectedTechnology))
            {
                filtered = filtered.Where(p => p.Technologies != null && p.Technologies.Any(t => string.Equals(t, viewModel.SelectedTechnology, StringComparison.OrdinalIgnoreCase)));
            }

            if (viewModel.SelectedYear.HasValue && viewModel.SelectedYear.Value > 0)
            {
                filtered = filtered.Where(p => p.Year == viewModel.SelectedYear.Value);
            }

            // Apply Sorting
            filtered = viewModel.SortBy switch
            {
                "oldest" => filtered.OrderBy(p => p.Year).ThenBy(p => p.CreatedAt),
                "az" => filtered.OrderBy(p => p.Title),
                "za" => filtered.OrderByDescending(p => p.Title),
                _ => filtered.OrderByDescending(p => p.Year).ThenByDescending(p => p.CreatedAt) // recent
            };

            var filteredList = filtered.ToList();
            viewModel.TotalCount = filteredList.Count;

            // Apply Pagination
            viewModel.Projects = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return viewModel;
        }

        public async Task<ProjectItem?> GetProjectByIdOrSlugAsync(string idOrSlug)
        {
            if (string.IsNullOrWhiteSpace(idOrSlug)) return null;

            var clean = idOrSlug.Trim();

            if (_context.IsConnected && _context.ProjectsCollection != null)
            {
                try
                {
                    FilterDefinition<ProjectItem> filter;
                    if (ObjectId.TryParse(clean, out _))
                    {
                        filter = Builders<ProjectItem>.Filter.Or(
                            Builders<ProjectItem>.Filter.Eq(p => p.Id, clean),
                            Builders<ProjectItem>.Filter.Eq(p => p.Slug, clean.ToLowerInvariant())
                        );
                    }
                    else
                    {
                        filter = Builders<ProjectItem>.Filter.Eq(p => p.Slug, clean.ToLowerInvariant());
                    }

                    var item = await _context.ProjectsCollection.Find(filter).FirstOrDefaultAsync();
                    if (item != null) return item;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to find project {IdOrSlug} in MongoDB", idOrSlug);
                }
            }

            lock (_lock)
            {
                return _fallbackProjects.FirstOrDefault(p =>
                    string.Equals(p.Id, clean, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Slug, clean, StringComparison.OrdinalIgnoreCase));
            }
        }

        public async Task<List<ProjectItem>> GetProjectsByMemberIdAsync(string memberId)
        {
            if (string.IsNullOrWhiteSpace(memberId)) return new List<ProjectItem>();

            var clean = memberId.Trim();

            if (_context.IsConnected && _context.ProjectsCollection != null)
            {
                try
                {
                    var filter = Builders<ProjectItem>.Filter.ElemMatch(p => p.TeamMembers, m => m.MemberId == clean);
                    var list = await _context.ProjectsCollection.Find(filter).ToListAsync();
                    if (list != null && list.Count > 0) return list;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to query member projects from MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackProjects
                    .Where(p => p.TeamMembers != null && p.TeamMembers.Any(m => string.Equals(m.MemberId, clean, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
        }

        public async Task<List<ProjectItem>> GetFeaturedProjectsAsync(int count = 3)
        {
            if (_context.IsConnected && _context.ProjectsCollection != null)
            {
                try
                {
                    var filter = Builders<ProjectItem>.Filter.Eq(p => p.Featured, true);
                    var items = await _context.ProjectsCollection.Find(filter).Limit(count).ToListAsync();
                    if (items != null && items.Count > 0) return items;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting featured projects from MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackProjects.Where(p => p.Featured).Take(count).ToList();
            }
        }

        public async Task<int> GetTotalProjectsCountAsync()
        {
            if (_context.IsConnected && _context.ProjectsCollection != null)
            {
                try
                {
                    return (int)await _context.ProjectsCollection.CountDocumentsAsync(FilterDefinition<ProjectItem>.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error counting projects in MongoDB");
                }
            }

            lock (_lock)
            {
                return _fallbackProjects.Count;
            }
        }

        private static List<ProjectItem> GenerateInitialProjects()
        {
            return new List<ProjectItem>
            {
                // 1. EcoTrack (Canonical Card 1)
                new ProjectItem
                {
                    Id = "66f100000000000000000001",
                    Slug = "ecotrack",
                    Title = "EcoTrack",
                    ShortDescription = "A web platform to help students track their carbon footprint and take actionable steps towards a greener campus.",
                    FullDescription = "EcoTrack is an intelligent campus sustainability platform engineered to gamify eco-conscious habits among students and faculty. By integrating real-time telemetry from campus energy consumption and meal choices, EcoTrack computes localized carbon offset indices and provides actionable recommendations to minimize waste.",
                    ImageUrl = "/images/projects/ecotrack.svg",
                    Category = "Web App",
                    SubCategory = "Sustainability",
                    Technologies = new List<string> { "React", "Node.js", "MongoDB" },
                    TeamName = "Team GreenCode",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000000", Name = "Hetvi Dholiya", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "Team Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000002", Name = "Aarav Patel", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Frontend Engineer" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000003", Name = "Sneha Shah", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "Backend Architect" }
                    },
                    Year = 2025,
                    GithubUrl = "https://github.com/ACM-AMTICS/ecotrack",
                    LiveDemoUrl = "https://ecotrack.amtics.acm.org",
                    Status = "Active",
                    Featured = true,
                    CreatedAt = new DateTime(2025, 2, 10, 10, 0, 0, DateTimeKind.Utc)
                },

                // 2. LearnMate (Canonical Card 2)
                new ProjectItem
                {
                    Id = "66f100000000000000000002",
                    Slug = "learnmate",
                    Title = "LearnMate",
                    ShortDescription = "An AI-powered study assistant that summarizes notes, generates quizzes and helps with exam preparation.",
                    FullDescription = "LearnMate harnesses state-of-the-art transformer architectures to digest complex lecture notes, syllabi, and reference textbooks. Students can interact with a specialized conversational agent to clarify conceptual ambiguities, receive auto-generated active recall quizzes, and track topical retention over semester milestones.",
                    ImageUrl = "/images/projects/learnmate.svg",
                    Category = "AI/ML",
                    SubCategory = "Education",
                    Technologies = new List<string> { "Python", "TensorFlow", "FastAPI" },
                    TeamName = "Team ByteBuddies",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000004", Name = "Rohan Mehta", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "ML Engineer" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000005", Name = "Diya Sharma", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "NLP Specialist" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000006", Name = "Karan Dave", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "Full Stack Dev" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/learnmate-ai",
                    LiveDemoUrl = "https://learnmate.amtics.acm.org",
                    Status = "Completed",
                    Featured = true,
                    CreatedAt = new DateTime(2024, 11, 15, 14, 0, 0, DateTimeKind.Utc)
                },

                // 3. AMTICS Navigator (Canonical Card 3)
                new ProjectItem
                {
                    Id = "66f100000000000000000003",
                    Slug = "amtics-navigator",
                    Title = "AMTICS Navigator",
                    ShortDescription = "A mobile app to help students navigate the AMTICS campus with indoor maps, event updates and important resources.",
                    FullDescription = "Built with Flutter and high-precision spatial mapping, AMTICS Navigator solves campus disorientation for freshmen and conference attendees. Features turn-by-turn indoor routing between laboratories, classrooms, and faculty offices, synchronized with live event schedules and building occupancy levels.",
                    ImageUrl = "/images/projects/navigator.svg",
                    Category = "Mobile App",
                    SubCategory = "Campus Utility",
                    Technologies = new List<string> { "Flutter", "Firebase", "Maps API" },
                    TeamName = "Team Trailblazers",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000007", Name = "Pranav Desai", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Mobile Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000008", Name = "Ananya Joshi", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "UI/UX Designer" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000009", Name = "Yash Trivedi", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Cloud & GIS" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/amtics-navigator",
                    LiveDemoUrl = "https://navigator.amtics.acm.org",
                    Status = "Completed",
                    Featured = true,
                    CreatedAt = new DateTime(2024, 8, 20, 11, 30, 0, DateTimeKind.Utc)
                },

                // 4. ShieldNet (Canonical Card 4)
                new ProjectItem
                {
                    Id = "66f100000000000000000004",
                    Slug = "shieldnet",
                    Title = "ShieldNet",
                    ShortDescription = "An open source network monitoring tool to detect suspicious activity and enhance campus network security.",
                    FullDescription = "ShieldNet is a low-overhead packet inspection and intrusion-detection framework designed for academic local area networks. By leveraging Scapy packet dissection and containerized detection workers, ShieldNet detects port sweeps, ARP poisoning, and rogue DHCP servers in real time with minimal resource footprint.",
                    ImageUrl = "/images/projects/shieldnet.svg",
                    Category = "Cybersecurity",
                    SubCategory = "Open Source",
                    Technologies = new List<string> { "Python", "Scapy", "Docker" },
                    TeamName = "Team SecureBits",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000000a", Name = "Hetvi Dholiya", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "Security Researcher" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000000b", Name = "Manan Parekh", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "Systems Architect" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000000c", Name = "Riddhi Parmar", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "DevOps" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/shieldnet",
                    LiveDemoUrl = "https://shieldnet.amtics.acm.org",
                    Status = "Completed",
                    Featured = true,
                    CreatedAt = new DateTime(2024, 6, 12, 16, 0, 0, DateTimeKind.Utc)
                },

                // 5. MindSpace (Canonical Card 5)
                new ProjectItem
                {
                    Id = "66f100000000000000000005",
                    Slug = "mindspace",
                    Title = "MindSpace",
                    ShortDescription = "A safe and anonymous platform for students to track their mood, access resources and connect with support communities.",
                    FullDescription = "MindSpace offers a compassionate, cryptographically anonymized mental wellness portal for university undergraduates. Incorporating guided journal prompts, mood trend dashboards, peer support forum rooms, and direct crisis counseling escalations, MindSpace fosters a culture of empathy across the campus community.",
                    ImageUrl = "/images/projects/mindspace.svg",
                    Category = "Web App",
                    SubCategory = "Health & Wellness",
                    Technologies = new List<string> { "React", "Node.js", "PostgreSQL" },
                    TeamName = "Team MindMates",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000000d", Name = "Bhavik Gandhi", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Frontend Lead" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000000e", Name = "Meera Soni", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "Backend Dev" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000000f", Name = "Devansh Vyas", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Product Designer" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/mindspace",
                    LiveDemoUrl = "https://mindspace.amtics.acm.org",
                    Status = "Completed",
                    Featured = true,
                    CreatedAt = new DateTime(2023, 10, 5, 9, 0, 0, DateTimeKind.Utc)
                },

                // 6. CodeCollab (Canonical Card 6)
                new ProjectItem
                {
                    Id = "66f100000000000000000006",
                    Slug = "codecollab",
                    Title = "CodeCollab",
                    ShortDescription = "A real-time collaborative code editor built for student teams with integrated chat and version control.",
                    FullDescription = "CodeCollab empowers engineering student pairs to code synchronously with sub-50ms operational transformation latency. Features Monaco Editor integration, syntax highlighting across 40+ programming languages, integrated WebRTC voice channels, and single-click commit synchronization to GitHub repositories.",
                    ImageUrl = "/images/projects/codecollab.svg",
                    Category = "Developer Tool",
                    SubCategory = "Productivity",
                    Technologies = new List<string> { "Next.js", "WebSockets", "MongoDB" },
                    TeamName = "Team DevSync",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000010", Name = "Tanmay Shah", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "Lead Architect" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000011", Name = "Isha Patel", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "WebSocket Specialist" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000012", Name = "Harshil Vora", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "UI Engineer" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/codecollab",
                    LiveDemoUrl = "https://codecollab.amtics.acm.org",
                    Status = "Completed",
                    Featured = true,
                    CreatedAt = new DateTime(2023, 8, 14, 15, 0, 0, DateTimeKind.Utc)
                },

                // 7. DisasterRelief AI (Canonical Card 7)
                new ProjectItem
                {
                    Id = "66f100000000000000000007",
                    Slug = "disasterrelief-ai",
                    Title = "DisasterRelief AI",
                    ShortDescription = "An AI-based system to analyze satellite images and identify disaster-affected areas for faster response.",
                    FullDescription = "Developed in collaboration with community rescue volunteers, DisasterRelief AI processes multimodal satellite multispectral feeds to segment flooded zones, structural collapses, and impassable roadways post-cyclone. Accelerated inference pipelines deliver rapid situational awareness summaries directly to emergency coordinators.",
                    ImageUrl = "/images/projects/disasterrelief.svg",
                    Category = "AI/ML",
                    SubCategory = "Social Impact",
                    Technologies = new List<string> { "Python", "OpenCV", "TensorFlow" },
                    TeamName = "Team Impact",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000013", Name = "Siddharth Rao", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Computer Vision Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000014", Name = "Pooja Solanki", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "GIS Researcher" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000015", Name = "Aryan Chawla", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Backend Dev" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/disasterrelief-ai",
                    LiveDemoUrl = "https://disasterrelief.amtics.acm.org",
                    Status = "Completed",
                    Featured = true,
                    CreatedAt = new DateTime(2023, 5, 22, 12, 0, 0, DateTimeKind.Utc)
                },

                // 8. EventHub (Canonical Card 8)
                new ProjectItem
                {
                    Id = "66f100000000000000000008",
                    Slug = "eventhub",
                    Title = "EventHub",
                    ShortDescription = "A central platform to discover, register and manage all ACM AMTICS events and workshops.",
                    FullDescription = "EventHub powers frictionless event management across the ACM AMTICS calendar. Providing RSVP ticketing, automatic dynamic QR check-ins, automated digital certificate generation, and post-session feedback analytics, EventHub has streamlined turnout for over 30 workshops.",
                    ImageUrl = "/images/projects/eventhub.svg",
                    Category = "Web App",
                    SubCategory = "Community",
                    Technologies = new List<string> { "React", "Firebase", "Tailwind CSS" },
                    TeamName = "Team Eventify",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000000", Name = "Hetvi Dholiya", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "Full Stack Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000016", Name = "Kavya Patel", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "Frontend Dev" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000017", Name = "Jay Rathod", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "Cloud Ops" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/eventhub",
                    LiveDemoUrl = "https://eventhub.amtics.acm.org",
                    Status = "Completed",
                    Featured = true,
                    CreatedAt = new DateTime(2023, 3, 10, 10, 0, 0, DateTimeKind.Utc)
                },

                // 9. Campus Connect
                new ProjectItem
                {
                    Id = "66f100000000000000000009",
                    Slug = "campus-connect",
                    Title = "Campus Connect",
                    ShortDescription = "A collaborative platform to connect students, share academic resources and form hackathon teams.",
                    FullDescription = "Campus Connect serves as a bridge for interdepartmental synergy. Students can post project open-calls, discover peer study cohorts, trade curated course summaries, and coordinate team submissions for regional hackathons.",
                    ImageUrl = "/images/projects/campus-connect.jpg",
                    Category = "Web App",
                    SubCategory = "Open Source",
                    Technologies = new List<string> { "Next.js", "Express", "MongoDB" },
                    TeamName = "Team ConnectX",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000018", Name = "Aman Verma", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Team Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000019", Name = "Shreya Joshi", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "Backend Dev" }
                    },
                    Year = 2025,
                    GithubUrl = "https://github.com/ACM-AMTICS/campus-connect",
                    LiveDemoUrl = "https://connect.amtics.acm.org",
                    Status = "Active",
                    Featured = false,
                    CreatedAt = new DateTime(2025, 1, 15, 8, 0, 0, DateTimeKind.Utc)
                },

                // 10. Amal Tracker
                new ProjectItem
                {
                    Id = "66f10000000000000000000a",
                    Slug = "amal-tracker",
                    Title = "Amal Tracker",
                    ShortDescription = "A productive habit-tracking mobile application designed with a clean, distraction-free interface.",
                    FullDescription = "Amal Tracker emphasizes purposeful habit formation through gentle positive reinforcement loops. Featuring customized streak metrics, offline-first SQLite synchronization, and calming minimalism.",
                    ImageUrl = "/images/projects/amal-tracker.jpg",
                    Category = "Mobile App",
                    SubCategory = "Productivity",
                    Technologies = new List<string> { "Flutter", "SQLite", "Dart" },
                    TeamName = "Team AmalDevs",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000001a", Name = "Farhan Khan", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Flutter Dev" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000001b", Name = "Zaid Sheikh", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "UI Designer" }
                    },
                    Year = 2025,
                    GithubUrl = "https://github.com/ACM-AMTICS/amal-tracker",
                    LiveDemoUrl = "https://amaltracker.amtics.acm.org",
                    Status = "Active",
                    Featured = false,
                    CreatedAt = new DateTime(2025, 1, 5, 9, 30, 0, DateTimeKind.Utc)
                },

                // 11. Neograb
                new ProjectItem
                {
                    Id = "66f10000000000000000000b",
                    Slug = "neograb",
                    Title = "Neograb",
                    ShortDescription = "A hyperlocal commerce platform connecting local campus stores and student buyers.",
                    FullDescription = "Neograb digitalizes campus retail outlets with live inventory search, fast digital payments, and pickup scheduling to eliminate dining and stationery queues during peak hours.",
                    ImageUrl = "/images/projects/neograb.jpg",
                    Category = "Web App",
                    SubCategory = "E-Commerce",
                    Technologies = new List<string> { "React", "Node.js", "MongoDB" },
                    TeamName = "Team NeoMinds",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000001c", Name = "Kunal Panchal", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "Full Stack Dev" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000001d", Name = "Priya Nambiar", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "Frontend Engineer" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/neograb",
                    LiveDemoUrl = "https://neograb.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2024, 12, 1, 14, 0, 0, DateTimeKind.Utc)
                },

                // 12. SmartAttendance
                new ProjectItem
                {
                    Id = "66f10000000000000000000c",
                    Slug = "smart-attendance",
                    Title = "SmartAttendance",
                    ShortDescription = "Contactless attendance system leveraging facial landmark embeddings for classroom sessions.",
                    FullDescription = "SmartAttendance automates student presence verification in large auditoriums via low-light camera streams and edge AI recognition models, securely compiling reports for academic coordinators.",
                    ImageUrl = "/images/projects/ecotrack.svg",
                    Category = "AI/ML",
                    SubCategory = "Campus Utility",
                    Technologies = new List<string> { "Python", "OpenCV", "FastAPI" },
                    TeamName = "Team CodeCrafters",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000001e", Name = "Nikhil Bhatt", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "AI Lead" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000001f", Name = "Tarun Sen", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "Embedded Dev" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/smart-attendance",
                    LiveDemoUrl = "https://attendance.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2024, 9, 10, 11, 0, 0, DateTimeKind.Utc)
                },

                // 13. CrypticAudit
                new ProjectItem
                {
                    Id = "66f10000000000000000000d",
                    Slug = "crypticaudit",
                    Title = "CrypticAudit",
                    ShortDescription = "Automated static analysis tool to uncover reentrancy and arithmetic vulnerabilities in smart contracts.",
                    FullDescription = "CrypticAudit parses abstract syntax trees of smart contracts to detect common exploit vectors, generating audit reports with suggested mitigation patches.",
                    ImageUrl = "/images/projects/shieldnet.svg",
                    Category = "Cybersecurity",
                    SubCategory = "Developer Tool",
                    Technologies = new List<string> { "Rust", "Solidity", "Docker" },
                    TeamName = "Team CyberShield",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000020", Name = "Ravi Shankar", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Security Researcher" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000021", Name = "Aditya Kulkarni", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "Rust Engineer" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/crypticaudit",
                    LiveDemoUrl = "https://audit.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2024, 7, 18, 16, 30, 0, DateTimeKind.Utc)
                },

                // 14. AgriDrone Vision
                new ProjectItem
                {
                    Id = "66f10000000000000000000e",
                    Slug = "agridrone-vision",
                    Title = "AgriDrone Vision",
                    ShortDescription = "Aerial drone crop surveillance system diagnosing plant disease outbreaks through multispectral vision.",
                    FullDescription = "Using lightweight edge models on autonomous quadcopters, AgriDrone Vision analyzes crop canopy health for rural farmers, preventing blight contagion.",
                    ImageUrl = "/images/projects/disasterrelief.svg",
                    Category = "AI/ML",
                    SubCategory = "Social Impact",
                    Technologies = new List<string> { "Python", "PyTorch", "OpenCV" },
                    TeamName = "Team AgroTech",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000022", Name = "Omkar Jadhav", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "ML Engineer" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000023", Name = "Swati Pillai", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "Robotics Specialist" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/agridrone",
                    LiveDemoUrl = "https://agridrone.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2024, 5, 25, 10, 15, 0, DateTimeKind.Utc)
                },

                // 15. CodeGrader
                new ProjectItem
                {
                    Id = "66f10000000000000000000f",
                    Slug = "codegrader",
                    Title = "CodeGrader",
                    ShortDescription = "Sandboxed automated programming homework evaluator with static linting and test execution.",
                    FullDescription = "CodeGrader runs student assignment solutions inside isolated Linux cgroups containers, validating runtime efficiency and adherence to style guides with instantaneous feedback.",
                    ImageUrl = "/images/projects/codecollab.svg",
                    Category = "Developer Tool",
                    SubCategory = "Education",
                    Technologies = new List<string> { "Go", "Docker", "PostgreSQL" },
                    TeamName = "Team DevGuild",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000024", Name = "Varun Kapoor", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Go Architect" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000025", Name = "Monika Nair", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "DevOps" }
                    },
                    Year = 2024,
                    GithubUrl = "https://github.com/ACM-AMTICS/codegrader",
                    LiveDemoUrl = "https://grader.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2024, 4, 10, 13, 0, 0, DateTimeKind.Utc)
                },

                // 16. HealthPulse
                new ProjectItem
                {
                    Id = "66f100000000000000000010",
                    Slug = "healthpulse",
                    Title = "HealthPulse",
                    ShortDescription = "Preventive health dashboard aggregating hydration, sleep cycles, and physical workout milestones.",
                    FullDescription = "HealthPulse bridges wearable fitness trackers with customizable health objectives, alerting students to fatigue patterns during exam periods.",
                    ImageUrl = "/images/projects/mindspace.svg",
                    Category = "Web App",
                    SubCategory = "Health & Wellness",
                    Technologies = new List<string> { "React", "Node.js", "Redis" },
                    TeamName = "Team PulseHealth",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000026", Name = "Deepak Somani", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Frontend Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000027", Name = "Sonia Chopra", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "Data Engineer" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/healthpulse",
                    LiveDemoUrl = "https://healthpulse.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2023, 11, 28, 17, 0, 0, DateTimeKind.Utc)
                },

                // 17. TrafficLens
                new ProjectItem
                {
                    Id = "66f100000000000000000011",
                    Slug = "trafficlens",
                    Title = "TrafficLens",
                    ShortDescription = "Computer vision traffic density estimator optimizing campus gate signals and parking slots.",
                    FullDescription = "TrafficLens feeds RTSP surveillance feeds into edge YOLO models to compute pedestrian crossway wait times and park space availability in real time.",
                    ImageUrl = "/images/projects/navigator.svg",
                    Category = "AI/ML",
                    SubCategory = "Campus Utility",
                    Technologies = new List<string> { "Python", "FastAPI", "OpenCV" },
                    TeamName = "Team Visionaries",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000028", Name = "Gaurav Sen", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "CV Engineer" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000029", Name = "Rashmi Tiwari", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "Embedded Lead" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/trafficlens",
                    LiveDemoUrl = "https://trafficlens.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2023, 9, 12, 10, 0, 0, DateTimeKind.Utc)
                },

                // 18. RoboNav
                new ProjectItem
                {
                    Id = "66f100000000000000000012",
                    Slug = "robonav",
                    Title = "RoboNav",
                    ShortDescription = "Autonomous indoor LiDAR mapping and SLAM navigation stack for educational robotics kits.",
                    FullDescription = "RoboNav delivers modular ROS2 packages enabling two-wheel differential drive robots to construct accurate 2D floor plans and navigate around obstacles.",
                    ImageUrl = "/images/projects/codecollab.svg",
                    Category = "Developer Tool",
                    SubCategory = "Open Source",
                    Technologies = new List<string> { "Python", "Docker", "Linux" },
                    TeamName = "Team MechaTech",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000002a", Name = "Chirag Joshi", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Robotics Lead" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000002b", Name = "Komal Pandey", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "Firmware Dev" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/robonav",
                    LiveDemoUrl = "https://robonav.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2023, 7, 2, 11, 45, 0, DateTimeKind.Utc)
                },

                // 19. VoiceAssist
                new ProjectItem
                {
                    Id = "66f100000000000000000013",
                    Slug = "voiceassist",
                    Title = "VoiceAssist",
                    ShortDescription = "Real-time speech-to-text live lecture captioning tool for hearing-impaired undergraduates.",
                    FullDescription = "VoiceAssist streams microphone audio to an optimized Whisper transcription engine, rendering low-latency subtitles and downloadable lecture transcripts.",
                    ImageUrl = "/images/projects/learnmate.svg",
                    Category = "AI/ML",
                    SubCategory = "Accessibility",
                    Technologies = new List<string> { "Python", "FastAPI", "WebSockets" },
                    TeamName = "Team SoundWave",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000002c", Name = "Suresh Raina", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Audio DSP Lead" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000002d", Name = "Neha Dubey", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "Accessibility UX" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/voiceassist",
                    LiveDemoUrl = "https://voiceassist.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2023, 4, 18, 14, 20, 0, DateTimeKind.Utc)
                },

                // 20. CloudVault
                new ProjectItem
                {
                    Id = "66f100000000000000000014",
                    Slug = "cloudvault",
                    Title = "CloudVault",
                    ShortDescription = "Zero-knowledge client-side encrypted cloud document storage system for student research papers.",
                    FullDescription = "CloudVault employs AES-GCM-256 client-side encryption and Argon2 key derivation so neither storage providers nor intermediaries can inspect academic files.",
                    ImageUrl = "/images/projects/shieldnet.svg",
                    Category = "Cybersecurity",
                    SubCategory = "Productivity",
                    Technologies = new List<string> { "React", "Node.js", "Docker" },
                    TeamName = "Team CloudLock",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a10000000000000000002e", Name = "Alok Dixit", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "Crypto Dev" },
                        new ProjectTeamMember { MemberId = "65a10000000000000000002f", Name = "Pooja Hegde", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "Full Stack Engineer" }
                    },
                    Year = 2023,
                    GithubUrl = "https://github.com/ACM-AMTICS/cloudvault",
                    LiveDemoUrl = "https://cloudvault.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2023, 2, 8, 16, 0, 0, DateTimeKind.Utc)
                },

                // 21. PeerMentor
                new ProjectItem
                {
                    Id = "66f100000000000000000015",
                    Slug = "peermentor",
                    Title = "PeerMentor",
                    ShortDescription = "Peer-to-peer tutoring scheduler matching junior students with verified senior mentors.",
                    FullDescription = "PeerMentor matches students struggling with data structures, operating systems, or math with senior mentors based on availability, ratings, and course history.",
                    ImageUrl = "/images/projects/eventhub.svg",
                    Category = "Web App",
                    SubCategory = "Education",
                    Technologies = new List<string> { "React", "Node.js", "MongoDB" },
                    TeamName = "Team PeerNet",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000030", Name = "Kiran Bedi", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Full Stack" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000031", Name = "Ashwin Murthy", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "Backend" }
                    },
                    Year = 2022,
                    GithubUrl = "https://github.com/ACM-AMTICS/peermentor",
                    LiveDemoUrl = "https://peermentor.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2022, 10, 20, 10, 0, 0, DateTimeKind.Utc)
                },

                // 22. StudySpot
                new ProjectItem
                {
                    Id = "66f100000000000000000016",
                    Slug = "studyspot",
                    Title = "StudySpot",
                    ShortDescription = "Library seat reservation and ambient noise monitoring mobile app for quiet study spaces.",
                    FullDescription = "StudySpot allows students to reserve study pods in the central library while checking live decibel noise readings from ESP32 sensors positioned in quiet study halls.",
                    ImageUrl = "/images/projects/amal-tracker.jpg",
                    Category = "Mobile App",
                    SubCategory = "Campus Utility",
                    Technologies = new List<string> { "Flutter", "Firebase", "Maps API" },
                    TeamName = "Team SpaceFinders",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000032", Name = "Rohit Verma", AvatarUrl = "/images/avatars/avatar-4.svg", Role = "Mobile Engineer" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000033", Name = "Divya Saxena", AvatarUrl = "/images/avatars/avatar-5.svg", Role = "IoT Specialist" }
                    },
                    Year = 2022,
                    GithubUrl = "https://github.com/ACM-AMTICS/studyspot",
                    LiveDemoUrl = "https://studyspot.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2022, 8, 15, 12, 0, 0, DateTimeKind.Utc)
                },

                // 23. AlgoVisualizer
                new ProjectItem
                {
                    Id = "66f100000000000000000017",
                    Slug = "algovisualizer",
                    Title = "AlgoVisualizer",
                    ShortDescription = "Interactive visualizer illustrating pathfinding, sorting, and graph algorithms step by step.",
                    FullDescription = "AlgoVisualizer provides an interactive canvas playground for students learning Dijkstra, A*, QuickSort, and Red-Black tree rebalancing with step-forward debugging and speed sliders.",
                    ImageUrl = "/images/projects/codecollab.svg",
                    Category = "Developer Tool",
                    SubCategory = "Education",
                    Technologies = new List<string> { "React", "Node.js", "Tailwind CSS" },
                    TeamName = "Team AlgoWizards",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000034", Name = "Manoj Bajpayee", AvatarUrl = "/images/avatars/avatar-6.svg", Role = "Algorithms Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000035", Name = "Kavita Reddy", AvatarUrl = "/images/avatars/avatar-1.jpg", Role = "UI Dev" }
                    },
                    Year = 2022,
                    GithubUrl = "https://github.com/ACM-AMTICS/algovisualizer",
                    LiveDemoUrl = "https://algo.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2022, 5, 14, 15, 30, 0, DateTimeKind.Utc)
                },

                // 24. EcoBin Smart Sensor
                new ProjectItem
                {
                    Id = "66f100000000000000000018",
                    Slug = "ecobin-smart-sensor",
                    Title = "EcoBin Smart Sensor",
                    ShortDescription = "IoT-connected ultrasonic waste bin fill-level monitoring system with automated route alerts.",
                    FullDescription = "EcoBin embeds ultrasonic distance sensors into campus recycling receptacles, transmitting fill telemetry over MQTT to optimize cleaning schedules.",
                    ImageUrl = "/images/projects/ecotrack.svg",
                    Category = "Web App",
                    SubCategory = "Sustainability",
                    Technologies = new List<string> { "Python", "FastAPI", "MongoDB" },
                    TeamName = "Team GreenSensors",
                    TeamMembers = new List<ProjectTeamMember>
                    {
                        new ProjectTeamMember { MemberId = "65a100000000000000000036", Name = "Vikas Khanna", AvatarUrl = "/images/avatars/avatar-2.svg", Role = "Embedded Lead" },
                        new ProjectTeamMember { MemberId = "65a100000000000000000037", Name = "Anita Roy", AvatarUrl = "/images/avatars/avatar-3.svg", Role = "Backend Dev" }
                    },
                    Year = 2022,
                    GithubUrl = "https://github.com/ACM-AMTICS/ecobin",
                    LiveDemoUrl = "https://ecobin.amtics.acm.org",
                    Status = "Completed",
                    Featured = false,
                    CreatedAt = new DateTime(2022, 2, 20, 11, 0, 0, DateTimeKind.Utc)
                }
            };
        }
    }
}
