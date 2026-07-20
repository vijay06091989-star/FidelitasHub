using FidelitasHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class ManagerApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Manager Approval List
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
        // Manager Approve
        //==================================================
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.LeaveApplicationId == id);

            if (leave == null)
                return NotFound();

            leave.ManagerStatus = "Approved";
            leave.ManagerApprovalDate = DateTime.Now;
            leave.Status = "Approved";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        //==================================================
        // Manager Reject
        //==================================================
        public async Task<IActionResult> Reject(int id)
        {
            var leave = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.LeaveApplicationId == id);

            if (leave == null)
                return NotFound();

            leave.ManagerStatus = "Rejected";
            leave.ManagerApprovalDate = DateTime.Now;
            leave.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}