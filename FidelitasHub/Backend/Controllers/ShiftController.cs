using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class ShiftController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MasterSequenceGenerator _sequenceGenerator;

        public ShiftController(
            ApplicationDbContext context,
            MasterSequenceGenerator sequenceGenerator)
        {
            _context = context;
            _sequenceGenerator = sequenceGenerator;
        }

        //==================================================
        // Shift List / Search / Status Filter
        //==================================================

        public IActionResult Index(string searchText, string status)
        {
            var shifts = _context.Shifts.AsQueryable();

            //=========================
            // Search
            //=========================

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                shifts = shifts.Where(s =>

                    s.ShiftCode.Contains(searchText) ||

                    s.ShiftName.Contains(searchText)

                );
            }

            //=========================
            // Status Filter
            //=========================

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active")
                {
                    shifts = shifts.Where(s => s.IsActive);
                }
                else if (status == "Inactive")
                {
                    shifts = shifts.Where(s => !s.IsActive);
                }
            }

            ViewBag.TotalShifts =
                _context.Shifts.Count();

            ViewBag.ActiveShifts =
                _context.Shifts.Count(s => s.IsActive);

            ViewBag.InactiveShifts =
                _context.Shifts.Count(s => !s.IsActive);

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;

            return View(shifts
                .OrderBy(s => s.ShiftCode)
                .ToList());
        }

        //==================================================
        // Add Shift (GET)
        //==================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        //==================================================
        // Add Shift (POST)
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Shift shift)
        {
            shift.ShiftCode =
                _sequenceGenerator.GenerateShiftCode();

            shift.IsActive = true;

            shift.CreatedDate = DateTime.Now;

            ModelState.Remove(nameof(Shift.ShiftCode));

            if (ModelState.IsValid)
            {
                bool exists = _context.Shifts.Any(s =>

                    s.ShiftName.Trim().ToLower() ==
                    shift.ShiftName.Trim().ToLower()

                );

                if (exists)
                {
                    ModelState.AddModelError(
                        "ShiftName",
                        "Shift already exists.");

                    return View(shift);
                }

                _context.Shifts.Add(shift);

                _context.SaveChanges();

                TempData["Success"] =
                    "Shift added successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(shift);
        }
        //==================================================
        // Edit Shift (GET)
        //==================================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var shift = _context.Shifts.Find(id);

            if (shift == null)
            {
                return NotFound();
            }

            return View(shift);
        }

        //==================================================
        // Edit Shift (POST)
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Shift shift)
        {
            ModelState.Remove(nameof(Shift.ShiftCode));

            if (ModelState.IsValid)
            {
                bool exists = _context.Shifts.Any(s =>

                    s.ShiftId != shift.ShiftId &&

                    s.ShiftName.Trim().ToLower() ==
                    shift.ShiftName.Trim().ToLower()

                );

                if (exists)
                {
                    ModelState.AddModelError(
                        "ShiftName",
                        "Shift already exists.");

                    return View(shift);
                }

                var existingShift =
                    _context.Shifts.Find(shift.ShiftId);

                if (existingShift == null)
                {
                    return NotFound();
                }

                existingShift.ShiftName = shift.ShiftName;
                existingShift.StandardStartTime = shift.StandardStartTime;
                existingShift.StandardEndTime = shift.StandardEndTime;

                existingShift.DstApplicable = shift.DstApplicable;
                existingShift.DstStartTime = shift.DstStartTime;
                existingShift.DstEndTime = shift.DstEndTime;

                existingShift.MinimumPunchInMinutes =
                    shift.MinimumPunchInMinutes;

                existingShift.GraceMinutes =
                    shift.GraceMinutes;

                existingShift.MaximumBreakMinutes =
                    shift.MaximumBreakMinutes;

                existingShift.MaximumPunchOutMinutes =
                    shift.MaximumPunchOutMinutes;

                existingShift.IsActive =
                    shift.IsActive;

                _context.SaveChanges();

                TempData["Success"] =
                    "Shift updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(shift);
        }
        //==================================================
        // Disable Shift
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(int id)
        {
            try
            {
                var shift = _context.Shifts.Find(id);

                if (shift == null)
                {
                    TempData["Error"] = "Shift not found.";
                    return RedirectToAction(nameof(Index));
                }

                int employeeCount = _context.Employees.Count(e =>
                    e.IsActive &&
                    e.ShiftId == shift.ShiftId);

                if (employeeCount > 0)
                {
                    TempData["Error"] =
                        $"Cannot disable Shift <b>{shift.ShiftName}</b>.<br><br>" +
                        $"It is assigned to <b>{employeeCount}</b> active employee(s).<br><br>" +
                        $"Please reassign or deactivate those employees first.";

                    return RedirectToAction(nameof(Index));
                }

                shift.IsActive = false;

                _context.SaveChanges();

                TempData["Success"] = "Shift disabled successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "<b>EXCEPTION:</b><br>" + ex.Message;

                return RedirectToAction(nameof(Index));
            }
        }

        //==================================================
        // Enable Shift
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(int id)
        {
            var shift = _context.Shifts.Find(id);

            if (shift == null)
            {
                return NotFound();
            }

            shift.IsActive = true;

            _context.SaveChanges();

            TempData["Success"] = "Shift enabled successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}