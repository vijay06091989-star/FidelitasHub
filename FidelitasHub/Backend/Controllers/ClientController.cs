using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FidelitasHub.Controllers
{
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientController(ApplicationDbContext context)
        {
            _context = context;
        }


        //==================================================
        // Client List / Search / Status Filter
        //==================================================

        public IActionResult Index(string? searchText, string? status)
        {
            var clients = _context.Clients.AsQueryable();


            //==================================================
            // Search
            //==================================================

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                clients = clients.Where(c =>
                    c.ClientCode.Contains(searchText) ||
                    c.ClientName.Contains(searchText));
            }


            //==================================================
            // Status Filter
            //==================================================

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active")
                {
                    clients = clients.Where(c => c.IsActive);
                }
                else if (status == "Inactive")
                {
                    clients = clients.Where(c => !c.IsActive);
                }
            }


            //==================================================
            // Dashboard Statistics
            //==================================================

            ViewBag.TotalClients = _context.Clients.Count();

            ViewBag.ActiveClients =
                _context.Clients.Count(c => c.IsActive);

            ViewBag.InactiveClients =
                _context.Clients.Count(c => !c.IsActive);

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;


            return View(
                clients
                    .OrderBy(c => c.ClientCode)
                    .ToList()
            );
        }


        //==================================================
        // Add Client - GET
        //==================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadEmployees();

            return View();
        }


        //==================================================
        // Add Client - POST
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Client client)
        {
            if (ModelState.IsValid)
            {
                client.ClientCode = client.ClientCode.Trim();
                client.ClientName = client.ClientName.Trim();

                bool exists = _context.Clients.Any(c =>
                    c.ClientCode.ToLower() ==
                    client.ClientCode.ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        nameof(Client.ClientCode),
                        "Client code already exists."
                    );

                    LoadEmployees(client);

                    return View(client);
                }


                client.IsActive = true;
                client.CreatedOn = DateTime.Now;


                _context.Clients.Add(client);

                _context.SaveChanges();


                TempData["Success"] =
                    "Client added successfully.";


                return RedirectToAction(nameof(Index));
            }


            LoadEmployees(client);

            return View(client);
        }


        //==================================================
        // Edit Client - GET
        //==================================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var client = _context.Clients.Find(id);

            if (client == null)
            {
                return NotFound();
            }


            LoadEmployees(client);

            return View(client);
        }


        //==================================================
        // Edit Client - POST
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Client client)
        {
            if (ModelState.IsValid)
            {
                client.ClientCode = client.ClientCode.Trim();
                client.ClientName = client.ClientName.Trim();


                //==================================================
                // Check duplicate Client Code
                //==================================================

                bool exists = _context.Clients.Any(c =>
                    c.ClientId != client.ClientId &&
                    c.ClientCode.ToLower() ==
                    client.ClientCode.ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        nameof(Client.ClientCode),
                        "Client code already exists."
                    );

                    LoadEmployees(client);

                    return View(client);
                }


                var existingClient =
                    _context.Clients.Find(client.ClientId);


                if (existingClient == null)
                {
                    return NotFound();
                }


                //==================================================
                // Client Information
                //==================================================

                existingClient.ClientCode =
                    client.ClientCode;

                existingClient.ClientName =
                    client.ClientName;


                //==================================================
                // General Shift Manager
                //==================================================

                existingClient.GeneralShiftManagerId =
                    client.GeneralShiftManagerId;


                //==================================================
                // General Shift Team Leader - Billing
                //==================================================

                existingClient.GeneralShiftBillingTeamLeaderId =
                    client.GeneralShiftBillingTeamLeaderId;


                //==================================================
                // General Shift Team Leader - Posting
                //==================================================

                existingClient.GeneralShiftPostingTeamLeaderId =
                    client.GeneralShiftPostingTeamLeaderId;


                //==================================================
                // General Shift Team Leader - DM
                //==================================================

                existingClient.GeneralShiftDMTeamLeaderId =
                    client.GeneralShiftDMTeamLeaderId;


                //==================================================
                // General Shift Team Leader - End to End
                //==================================================

                existingClient.GeneralShiftEndToEndTeamLeaderId =
                    client.GeneralShiftEndToEndTeamLeaderId;


                //==================================================
                // US Shift Manager
                //==================================================

                existingClient.USShiftManagerId =
                    client.USShiftManagerId;


                //==================================================
                // US Shift Team Leader
                //==================================================

                existingClient.USShiftTeamLeaderId =
                    client.USShiftTeamLeaderId;


                //==================================================
                // Status
                //==================================================

                existingClient.IsActive =
                    client.IsActive;


                //==================================================
                // Audit
                //==================================================

                existingClient.ModifiedOn =
                    DateTime.Now;


                _context.SaveChanges();


                TempData["Success"] =
                    "Client updated successfully.";


                return RedirectToAction(nameof(Index));
            }


            LoadEmployees(client);

            return View(client);
        }


        //==================================================
        // Disable Client
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(int id)
        {
            var client = _context.Clients.Find(id);

            if (client == null)
            {
                TempData["Error"] =
                    "Client not found.";

                return RedirectToAction(nameof(Index));
            }


            client.IsActive = false;

            client.ModifiedOn = DateTime.Now;


            _context.SaveChanges();


            TempData["Success"] =
                "Client disabled successfully.";


            return RedirectToAction(nameof(Index));
        }


        //==================================================
        // Enable Client
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(int id)
        {
            var client = _context.Clients.Find(id);

            if (client == null)
            {
                return NotFound();
            }


            client.IsActive = true;

            client.ModifiedOn = DateTime.Now;


            _context.SaveChanges();


            TempData["Success"] =
                "Client enabled successfully.";


            return RedirectToAction(nameof(Index));
        }


        //==================================================
        // Employee Dropdowns
        //==================================================

        private void LoadEmployees(Client? client = null)
        {
            //==================================================
            // Managers
            // Admin + Manager Roles
            //==================================================

            var managers = _context.Employees
                .Where(e =>
                    e.IsActive &&
                    (
                        e.Role == "Admin" ||
                        e.Role == "SuperAdmin" ||
                        e.Role == "Manager"
                    ))
                .OrderBy(e => e.EmployeeName)
                .ToList();


            //==================================================
            // Team Leaders
            //==================================================

            var teamLeaders = _context.Employees
                .Where(e =>
                    e.IsActive &&
                    e.Role == "Team Leader")
                .OrderBy(e => e.EmployeeName)
                .ToList();


            //==================================================
            // General Shift Manager
            //==================================================

            ViewBag.GeneralShiftManagers =
                new SelectList(
                    managers,
                    "EmployeeId",
                    "EmployeeName",
                    client?.GeneralShiftManagerId
                );


            //==================================================
            // General Shift Team Leader - Billing
            //==================================================

            ViewBag.GeneralShiftBillingTeamLeaders =
                new SelectList(
                    teamLeaders,
                    "EmployeeId",
                    "EmployeeName",
                    client?.GeneralShiftBillingTeamLeaderId
                );


            //==================================================
            // General Shift Team Leader - Posting
            //==================================================

            ViewBag.GeneralShiftPostingTeamLeaders =
                new SelectList(
                    teamLeaders,
                    "EmployeeId",
                    "EmployeeName",
                    client?.GeneralShiftPostingTeamLeaderId
                );


            //==================================================
            // General Shift Team Leader - DM
            //==================================================

            ViewBag.GeneralShiftDMTeamLeaders =
                new SelectList(
                    teamLeaders,
                    "EmployeeId",
                    "EmployeeName",
                    client?.GeneralShiftDMTeamLeaderId
                );


            //==================================================
            // General Shift Team Leader - End to End
            //==================================================

            ViewBag.GeneralShiftEndToEndTeamLeaders =
                new SelectList(
                    teamLeaders,
                    "EmployeeId",
                    "EmployeeName",
                    client?.GeneralShiftEndToEndTeamLeaderId
                );


            //==================================================
            // US Shift Manager
            //==================================================

            ViewBag.USShiftManagers =
                new SelectList(
                    managers,
                    "EmployeeId",
                    "EmployeeName",
                    client?.USShiftManagerId
                );


            //==================================================
            // US Shift Team Leader
            //==================================================

            ViewBag.USShiftTeamLeaders =
                new SelectList(
                    teamLeaders,
                    "EmployeeId",
                    "EmployeeName",
                    client?.USShiftTeamLeaderId
                );
        }
    }
}