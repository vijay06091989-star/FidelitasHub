using FidelitasHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FidelitasHub.Models;
using FidelitasHub.Services.Security;

namespace FidelitasHub.Controllers
{
    public class ProductivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IClientWebLoginProtectionService _webLoginProtection;

        public ProductivityController(
            ApplicationDbContext context,
            IClientWebLoginProtectionService webLoginProtection)
        {
            _context = context;
            _webLoginProtection = webLoginProtection;
        }


        //==================================================
        // Productivity Dashboard
        //==================================================

        public IActionResult Index()
        {
            return View();
        }


        //==================================================
        // Individual Client Productivity
        //==================================================

        public IActionResult Client(int id)
        {
            var client = _context.Clients

                // General Shift
                .Include(c => c.GeneralShiftManager)

                .Include(c => c.GeneralShiftBillingTeamLeader)

                .Include(c => c.GeneralShiftPostingTeamLeader)

                .Include(c => c.GeneralShiftDMTeamLeader)

                .Include(c => c.GeneralShiftEndToEndTeamLeader)


                // US Shift
                .Include(c => c.USShiftManager)

                .Include(c => c.USShiftTeamLeader)


                // Selected Client
                .FirstOrDefault(c => c.ClientId == id);


            if (client == null)
            {
                return NotFound();
            }


            ViewBag.CurrentSop = _context.ClientSopDocuments
                .Where(s => s.ClientId == client.ClientId && s.IsCurrent)
                .OrderByDescending(s => s.Version)
                .FirstOrDefault();


            return View(client);
        }


        //==================================================
        // Client Web Logins
        //==================================================

        public IActionResult WebLogins(int id)
        {
            var client = _context.Clients
                .FirstOrDefault(c => c.ClientId == id);

            if (client == null)
            {
                return NotFound();
            }

            var webLogins = _context.ClientWebLogins
                .Where(w => w.ClientId == id)
                .OrderBy(w => w.Website)
                .ThenBy(w => w.Username)
                .ToList()
                .Select(w =>
                {
                    var passwordAvailable =
                        _webLoginProtection.TryUnprotect(
                            w.EncryptedPassword,
                            out var password);

                    var securityAvailable =
                        _webLoginProtection.TryUnprotect(
                            w.EncryptedSecurityQuestions,
                            out var securityQuestions);

                    return new ClientWebLoginDisplayViewModel
                    {
                        ClientWebLoginId = w.ClientWebLoginId,
                        ClientId = w.ClientId,
                        Website = w.Website,
                        Url = w.Url,
                        Username = w.Username,
                        Password = passwordAvailable
                            ? password
                            : "[Unavailable]",
                        SecurityQuestions = securityAvailable
                            ? securityQuestions
                            : "[Unavailable]"
                    };
                })
                .ToList();

            ViewBag.WebLogins = webLogins;

            return View(client);
        }


        //==================================================
        // Productivity Setup
        //==================================================

        public IActionResult Setup()
        {
            return View();
        }


        //==================================================
        // Productivity Register
        //==================================================

        public IActionResult Register()
        {
            return View();
        }


        //==================================================
        // Productivity Reports
        //==================================================

        public IActionResult Reports()
        {
            return View();
        }
    }
}