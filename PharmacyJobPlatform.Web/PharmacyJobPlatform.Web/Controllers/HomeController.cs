using Microsoft.AspNetCore.Mvc;

namespace PharmacyJobPlatform.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
