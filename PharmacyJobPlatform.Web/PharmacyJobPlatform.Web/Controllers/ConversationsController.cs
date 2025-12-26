using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyJobPlatform.Domain.Entities;
using PharmacyJobPlatform.Infrastructure.Data;
using System.Security.Claims;

namespace PharmacyJobPlatform.Web.Controllers
{
    [Authorize]
    public class ConversationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConversationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var conversations = _context.Conversations
                .Include(x => x.User1)
                .Include(x => x.User2)
                .Where(x => x.User1Id == userId || x.User2Id == userId)
                .ToList();

            return View(conversations);
        }
    }

}
