using FidelitasHub.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace FidelitasHub.Controllers
{
    public class SystemSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var settings = _context.SystemSettings
                                   .OrderBy(s => s.SettingKey)
                                   .ToList();

            return View(settings);
        }
    }
}