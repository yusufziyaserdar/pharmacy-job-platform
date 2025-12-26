using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerDashboardController : Controller
    {
        public IActionResult Index() => View();
    }

}
