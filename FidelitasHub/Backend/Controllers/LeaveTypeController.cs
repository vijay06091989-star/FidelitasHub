using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class LeaveTypeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MasterSequenceGenerator _sequenceGenerator;

        public LeaveTypeController(
            ApplicationDbContext context,
            MasterSequenceGenerator sequenceGenerator)
        {
            _context = context;
            _sequenceGenerator = sequenceGenerator;
        }

        public IActionResult Index(string searchText, string status)
        {
            var leaveTypes = _context.LeaveTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                leaveTypes = leaveTypes.Where(l =>
                    l.LeaveTypeCode.Contains(searchText) ||
                    l.LeaveTypeName.Contains(searchText) ||
                    l.ShortCode.Contains(searchText));
            }

            if (status == "Active")
            {
                leaveTypes = leaveTypes.Where(l => l.IsActive);
            }
            else if (status == "Inactive")
            {
                leaveTypes = leaveTypes.Where(l => !l.IsActive);
            }

            ViewBag.TotalLeaveTypes = _context.LeaveTypes.Count();
            ViewBag.ActiveLeaveTypes = _context.LeaveTypes.Count(l => l.IsActive);
            ViewBag.InactiveLeaveTypes = _context.LeaveTypes.Count(l => !l.IsActive);
            ViewBag.SearchText = searchText;
            ViewBag.Status = status;

            return View(leaveTypes.OrderBy(l => l.LeaveTypeCode).ToList());
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LeaveType leaveType)
        {
            leaveType.LeaveTypeCode = _sequenceGenerator.GenerateLeaveTypeCode();
            leaveType.IsActive = true;
            leaveType.CreatedDate = DateTime.Now;

            ModelState.Remove(nameof(LeaveType.LeaveTypeCode));

            if (ModelState.IsValid)
            {
                bool exists = _context.LeaveTypes.Any(l =>
                    l.LeaveTypeName.Trim().ToLower() ==
                    leaveType.LeaveTypeName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError("LeaveTypeName", "Leave type already exists.");
                    return View(leaveType);
                }

                _context.LeaveTypes.Add(leaveType);
                _context.SaveChanges();

                TempData["Success"] = "Leave type added successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(leaveType);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var leaveType = _context.LeaveTypes.Find(id);
            if (leaveType == null) return NotFound();
            return View(leaveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(LeaveType leaveType)
        {
            if (ModelState.IsValid)
            {
                bool exists = _context.LeaveTypes.Any(l =>
                    l.LeaveTypeId != leaveType.LeaveTypeId &&
                    l.LeaveTypeName.Trim().ToLower() ==
                    leaveType.LeaveTypeName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError("LeaveTypeName", "Leave type already exists.");
                    return View(leaveType);
                }

                leaveType.ModifiedDate = DateTime.Now;
                _context.LeaveTypes.Update(leaveType);
                _context.SaveChanges();

                TempData["Success"] = "Leave type updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(leaveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(int id)
        {
            var leaveType = _context.LeaveTypes.Find(id);
            if (leaveType == null) return NotFound();

            leaveType.IsActive = false;
            _context.SaveChanges();

            TempData["Success"] = "Leave type disabled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(int id)
        {
            var leaveType = _context.LeaveTypes.Find(id);
            if (leaveType == null) return NotFound();

            leaveType.IsActive = true;
            _context.SaveChanges();

            TempData["Success"] = "Leave type enabled successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
