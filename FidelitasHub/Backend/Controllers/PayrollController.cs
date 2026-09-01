using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================================================
        // PAYROLL CALENDAR LIST
        // ==================================================

        public async Task<IActionResult> Index()
        {
            var calendars = await _context.PayrollCalendars
                .OrderByDescending(p => p.PeriodStart)
                .ToListAsync();

            return View(calendars);
        }


        // ==================================================
        // CREATE
        // ==================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PayrollCalendar());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PayrollCalendar payrollCalendar)
        {
            if (payrollCalendar.PeriodEnd < payrollCalendar.PeriodStart)
            {
                ModelState.AddModelError(
                    "PeriodEnd",
                    "Period End cannot be earlier than Period Start.");
            }

            bool overlaps = await _context.PayrollCalendars.AnyAsync(p =>
                payrollCalendar.PeriodStart <= p.PeriodEnd &&
                payrollCalendar.PeriodEnd >= p.PeriodStart);

            if (overlaps)
            {
                ModelState.AddModelError(
                    "",
                    "This payroll period overlaps with an existing payroll cycle.");
            }

            if (!ModelState.IsValid)
            {
                return View(payrollCalendar);
            }

            _context.PayrollCalendars.Add(payrollCalendar);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Payroll cycle created successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ==================================================
        // EDIT
        // ==================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var payrollCalendar = await _context.PayrollCalendars.FindAsync(id);

            if (payrollCalendar == null)
            {
                return NotFound();
            }

            return View(payrollCalendar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            PayrollCalendar payrollCalendar)
        {
            if (id != payrollCalendar.PayrollCalendarId)
            {
                return NotFound();
            }

            if (payrollCalendar.PeriodEnd < payrollCalendar.PeriodStart)
            {
                ModelState.AddModelError(
                    "PeriodEnd",
                    "Period End cannot be earlier than Period Start.");
            }

            bool overlaps = await _context.PayrollCalendars
                .AnyAsync(p =>
                    p.PayrollCalendarId != id &&
                    payrollCalendar.PeriodStart <= p.PeriodEnd &&
                    payrollCalendar.PeriodEnd >= p.PeriodStart);

            if (overlaps)
            {
                ModelState.AddModelError(
                    "",
                    "This payroll period overlaps with an existing payroll cycle.");
            }

            if (!ModelState.IsValid)
            {
                return View(payrollCalendar);
            }

            _context.Update(payrollCalendar);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Payroll cycle updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // ==================================================
        // DELETE
        // ==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var payrollCalendar = await _context.PayrollCalendars.FindAsync(id);

            if (payrollCalendar == null)
            {
                return NotFound();
            }

            _context.PayrollCalendars.Remove(payrollCalendar);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Payroll cycle deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}