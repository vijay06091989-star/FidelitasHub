using FidelitasHub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class ProductivityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductivityController(ApplicationDbContext context)
        {
            _context = context;
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