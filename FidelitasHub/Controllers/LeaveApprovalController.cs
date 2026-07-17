using FidelitasHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class LeaveApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var leaves = await _context.LeaveApplications
                .OrderByDescending(x => x.AppliedOn)
                .ToListAsync();

            return View(leaves);
        }
    }
}