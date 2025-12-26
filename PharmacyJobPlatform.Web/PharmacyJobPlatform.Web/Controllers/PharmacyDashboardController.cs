using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize(Roles = "PharmacyOwner")]
    public class PharmacyDashboardController : Controller
    {
        public IActionResult Index() => View();
    }


}
