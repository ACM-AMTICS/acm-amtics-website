using System.Security.Claims;
using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly IMongoDbContext _mongoDbContext;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IProfileService profileService,
            IMongoDbContext mongoDbContext,
            ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _mongoDbContext = mongoDbContext;
            _logger = logger;
        }

        // GET: /profile (Own Profile)
        [HttpGet]
        [Route("profile")]
        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                userEmail = User.Identity?.Name;
            }

            if (string.IsNullOrWhiteSpace(userEmail) || !userEmail.Contains("@"))
            {
                userEmail = "adminauth@gmail.com";
            }

            var vm = await _profileService.GetProfileViewModelAsync(userEmail, isOwnProfile: true);

            ViewBag.ActiveMenu = "Profile";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            ViewBag.MongoDatabase = _mongoDbContext.DatabaseName;
            ViewBag.MongoConnection = _mongoDbContext.ConnectionString;

            return View(vm);
        }

        // GET: /profile/:useremail (Public Profile)
        [AllowAnonymous]
        [HttpGet]
        [Route("profile/{useremail}")]
        [Route("MemberProfile")]
        [Route("Members/Profile")]
        [Route("Profile/Member")]
        [Route("Profile/Member/{useremail}")]
        public async Task<IActionResult> PublicProfile(string? useremail)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(useremail))
            {
                if (!string.IsNullOrWhiteSpace(currentUserEmail))
                {
                    useremail = UserProfile.GenerateSlug(currentUserEmail);
                }
                else
                {
                    useremail = "hetvi022";
                }
            }

            var cleanSlug = UserProfile.GenerateSlug(useremail);
            var isOwnProfile = !string.IsNullOrWhiteSpace(currentUserEmail) &&
                               (currentUserEmail.Equals(useremail, StringComparison.OrdinalIgnoreCase) ||
                                UserProfile.GenerateSlug(currentUserEmail).Equals(cleanSlug, StringComparison.OrdinalIgnoreCase));

            var vm = await _profileService.GetProfileViewModelBySlugAsync(cleanSlug, isOwnProfile);
            if (vm == null)
            {
                // Fallback attempt by direct email search
                vm = await _profileService.GetProfileViewModelAsync(useremail, isOwnProfile);
            }

            ViewBag.ActiveMenu = "Heads";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            ViewBag.MemberSlug = cleanSlug;

            return View("MemberProfile", vm);
        }

        // POST: /profile/edit (Edit Profile Endpoint)
        [HttpPost]
        [Route("profile/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] EditProfileDto dto)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return Json(new { success = false, message = "User identity not found." });
            }

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid profile data provided." });
            }

            try
            {
                var updated = await _profileService.UpdateProfileAsync(userEmail, dto);
                if (updated != null)
                {
                    return Json(new { success = true, message = "Profile updated successfully!", profile = updated });
                }
                return Json(new { success = false, message = "Failed to update profile." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for {Email}", userEmail);
                return Json(new { success = false, message = "An error occurred while saving profile changes." });
            }
        }
    }
}
