using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IMongoDbContext _mongoDbContext;

        public ProjectsController(IMongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
        }

        [HttpGet]
        [Route("Projects")]
        [Route("Admin/Projects")]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Projects";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View();
        }
    }
}
