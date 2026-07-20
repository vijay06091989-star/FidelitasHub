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

        //==================================================
        // Leave Approval List
        //==================================================
        public async Task<IActionResult> Index()
        {
            var leaves = await _context.LeaveApplications
                .Include(x => x.Employee)
                .OrderByDescending(x => x.AppliedOn)
                .ToListAsync();

            return View(leaves);
        }

        //==================================================
        // Team Leader Approve
        //==================================================
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.LeaveApplicationId == id);

            if (leave == null)
                return NotFound();

            leave.TeamLeaderStatus = "Approved";
            leave.TeamLeaderApprovalDate = DateTime.Now;

            leave.Status = "Pending Manager Approval";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave approved successfully and forwarded to Manager.";

            return RedirectToAction(nameof(Index));
        }

        //==================================================
        // Team Leader Reject
        //==================================================
        public async Task<IActionResult> Reject(int id)
        {
            var leave = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.LeaveApplicationId == id);

            if (leave == null)
                return NotFound();

            leave.TeamLeaderStatus = "Rejected";
            leave.TeamLeaderApprovalDate = DateTime.Now;

            leave.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave rejected successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}