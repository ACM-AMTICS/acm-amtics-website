using acm_amtics_website.Services;
using Microsoft.AspNetCore.Mvc;

namespace acm_amtics_website.Controllers
{
    public class MembersController : Controller
    {
        private readonly IMongoDbContext _mongoDbContext;

        public MembersController(IMongoDbContext mongoDbContext)
        {
            _mongoDbContext = mongoDbContext;
        }

        [HttpGet]
        [Route("Members")]
        public IActionResult Index()
        {
            ViewBag.ActiveMenu = "Members";
            ViewBag.ShowAddMemberButton = false; // Strictly disabled and omitted on /Members
            ViewBag.ViewContext = "Members";
            ViewBag.IsMongoConnected = _mongoDbContext.IsConnected;
            return View();
        }
    }
}
