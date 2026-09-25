using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IMongoDbContext _mongoDbContext;

        public ProfileController(IMongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
        }

        [HttpGet]
        [Route("Profile")]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Profile";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            ViewBag.MongoDatabase = _mongoDbContext.DatabaseName;
            ViewBag.MongoConnection = _mongoDbContext.ConnectionString;
            return View();
        }
    }
}
