using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IMongoDbContext _mongoDbContext;
        private readonly IMemberService _memberService;
        private readonly IProjectService _projectService;

        public ProfileController(
            IMongoDbContext mongoDbContext,
            IMemberService memberService,
            IProjectService projectService)
        {
            _mongoDbContext = mongoDbContext;
            _memberService = memberService;
            _projectService = projectService;
        }

        // GET: /Profile (Admin Profile view when authenticated)
        [HttpGet]
        [Route("Profile")]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Profile";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            ViewBag.MongoDatabase = _mongoDbContext.DatabaseName;
            ViewBag.MongoConnection = _mongoDbContext.ConnectionString;

            // If not logged in, redirect to login
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // GET: /Profile/Member/{id}
        // GET: /Member/{id}
        [HttpGet]
        [Route("Profile/Member/{id}")]
        [Route("Member/{id}")]
        public async Task<IActionResult> MemberProfile(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Projects");
            }

            var member = await _memberService.GetMemberByIdAsync(id);
            if (member == null)
            {
                // Fallback: create friendly public member profile based on ID
                member = new Member
                {
                    Id = id,
                    Name = "ACM AMTICS Member",
                    Role = "Contributor",
                    EnrollmentNumber = "AMTICS-" + id.Substring(0, Math.Min(6, id.Length)),
                    Status = "Active"
                };
            }

            var projects = await _projectService.GetProjectsByMemberIdAsync(id);

            var viewModel = new PublicMemberProfileViewModel
            {
                Member = member,
                Projects = projects,
                Department = "Department of Computer Science & Engineering, AMTICS",
                Bio = $"Active contributor and tech enthusiast in the ACM AMTICS Student Chapter. Participated in chapter hackathons, collaborative student engineering projects, and technical workshops.",
                Skills = new List<string> { "Full Stack", "Problem Solving", "Collaboration", "Open Source" }
            };

            ViewBag.ActiveMenu = "Projects";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View("Public", viewModel);
        }
    }
}
