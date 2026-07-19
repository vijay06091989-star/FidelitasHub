using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class HolidayController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HolidayController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? year, string status)
        {
            int selectedYear = year ?? DateTime.Today.Year;

            var holidays = _context.Holidays
                .Where(h => h.HolidayDate.Year == selectedYear)
                .AsQueryable();

            if (status == "Active")
            {
                holidays = holidays.Where(h => h.IsActive);
            }
            else if (status == "Inactive")
            {
                holidays = holidays.Where(h => !h.IsActive);
            }

            ViewBag.Year = selectedYear;
            ViewBag.Years = Enumerable.Range(DateTime.Today.Year - 2, 5).ToList();
            ViewBag.Status = status;
            ViewBag.TotalHolidays = _context.Holidays.Count(h => h.HolidayDate.Year == selectedYear);
            ViewBag.UpcomingHolidays = _context.Holidays.Count(h =>
                h.IsActive && h.HolidayDate >= DateTime.Today);

            return View(holidays.OrderBy(h => h.HolidayDate).ToList());
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Holiday holiday)
        {
            holiday.IsActive = true;
            holiday.CreatedDate = DateTime.Now;

            if (ModelState.IsValid)
            {
                bool exists = _context.Holidays.Any(h => h.HolidayDate == holiday.HolidayDate);

                if (exists)
                {
                    ModelState.AddModelError("HolidayDate", "A holiday already exists on this date.");
                    return View(holiday);
                }

                _context.Holidays.Add(holiday);
                _context.SaveChanges();

                TempData["Success"] = "Holiday added successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(holiday);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var holiday = _context.Holidays.Find(id);
            if (holiday == null) return NotFound();
            return View(holiday);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Holiday holiday)
        {
            if (ModelState.IsValid)
            {
                _context.Holidays.Update(holiday);
                _context.SaveChanges();

                TempData["Success"] = "Holiday updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(holiday);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(int id)
        {
            var holiday = _context.Holidays.Find(id);
            if (holiday == null) return NotFound();

            holiday.IsActive = false;
            _context.SaveChanges();

            TempData["Success"] = "Holiday disabled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(int id)
        {
            var holiday = _context.Holidays.Find(id);
            if (holiday == null) return NotFound();

            holiday.IsActive = true;
            _context.SaveChanges();

            TempData["Success"] = "Holiday enabled successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
