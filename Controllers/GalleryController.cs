using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class GalleryController : Controller
    {
        private readonly IMongoDbContext _mongoDbContext;

        public GalleryController(IMongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
        }

        [HttpGet]
        [Route("Gallery")]
        [Route("Admin/Gallery")]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Gallery";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View();
        }
    }
}
