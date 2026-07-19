using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PayrollController(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Payroll Calendar
        //==================================================

        public IActionResult Index()
        {
            var payrolls = _context.PayrollCalendars
                .OrderBy(p => p.PeriodStart)
                .ToList();

            return View(payrolls);
        }
    }
}