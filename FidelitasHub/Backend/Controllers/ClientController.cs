using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ClientController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            LoadSopDocuments(client.ClientId);

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
                    LoadSopDocuments(client.ClientId);

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
            LoadSopDocuments(client.ClientId);

            return View(client);
        }


        //==================================================
        // Upload New SOP Version
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadSop(int clientId, IFormFile? sopFile)
        {
            var client = await _context.Clients.FindAsync(clientId);

            if (client == null)
            {
                TempData["Error"] = "Client not found.";
                return RedirectToAction(nameof(Index));
            }

            if (sopFile == null || sopFile.Length == 0)
            {
                TempData["Error"] = "Please select an SOP PDF file to upload.";
                return RedirectToAction(nameof(Edit), new { id = clientId });
            }

            const long maxFileSize = 25 * 1024 * 1024;

            if (sopFile.Length > maxFileSize)
            {
                TempData["Error"] = "SOP file size cannot exceed 25 MB.";
                return RedirectToAction(nameof(Edit), new { id = clientId });
            }

            var extension = Path.GetExtension(sopFile.FileName);

            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only PDF files are supported for SOP upload.";
                return RedirectToAction(nameof(Edit), new { id = clientId });
            }

            // Verify the file really starts with the PDF signature.
            await using (var signatureStream = sopFile.OpenReadStream())
            {
                var signature = new byte[5];
                var bytesRead = await signatureStream.ReadAsync(signature);

                if (bytesRead != 5 ||
                    signature[0] != (byte)'%' ||
                    signature[1] != (byte)'P' ||
                    signature[2] != (byte)'D' ||
                    signature[3] != (byte)'F' ||
                    signature[4] != (byte)'-')
                {
                    TempData["Error"] = "The uploaded file is not a valid PDF document.";
                    return RedirectToAction(nameof(Edit), new { id = clientId });
                }
            }

            var nextVersion =
                (await _context.ClientSopDocuments
                    .Where(s => s.ClientId == clientId)
                    .Select(s => (int?)s.Version)
                    .MaxAsync() ?? 0) + 1;

            var safeClientName = MakeSafeFileName(client.ClientName);
            var displayFileName = $"{safeClientName}_SOP_V{nextVersion}.pdf";

            var relativeDirectory = Path.Combine("App_Data", "SOP", clientId.ToString());
            var absoluteDirectory = Path.Combine(_environment.ContentRootPath, relativeDirectory);
            Directory.CreateDirectory(absoluteDirectory);

            var storedFileName = $"SOP_V{nextVersion}_{Guid.NewGuid():N}.pdf";
            var absolutePath = Path.Combine(absoluteDirectory, storedFileName);
            var relativePath = Path.Combine(relativeDirectory, storedFileName).Replace('\\', '/');

            try
            {
                await using (var output = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await sopFile.CopyToAsync(output);
                }

                var existingCurrent = await _context.ClientSopDocuments
                    .Where(s => s.ClientId == clientId && s.IsCurrent)
                    .ToListAsync();

                foreach (var sop in existingCurrent)
                {
                    sop.IsCurrent = false;
                }

                var uploadedBy = HttpContext.Session.GetString("EmployeeName");

                if (string.IsNullOrWhiteSpace(uploadedBy))
                {
                    uploadedBy = HttpContext.Session.GetString("EmployeeCode");
                }

                if (string.IsNullOrWhiteSpace(uploadedBy))
                {
                    uploadedBy = "System";
                }

                var document = new ClientSopDocument
                {
                    ClientId = clientId,
                    Version = nextVersion,
                    DisplayFileName = displayFileName,
                    StoredFilePath = relativePath,
                    ContentType = "application/pdf",
                    FileSizeBytes = sopFile.Length,
                    UploadedBy = uploadedBy,
                    UploadedOn = DateTime.Now,
                    IsCurrent = true
                };

                _context.ClientSopDocuments.Add(document);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"SOP Version {nextVersion} uploaded successfully.";
            }
            catch
            {
                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }

                throw;
            }

            return RedirectToAction(nameof(Edit), new { id = clientId });
        }


        //==================================================
        // View SOP Document
        //==================================================

        [HttpGet]
        public async Task<IActionResult> ViewSop(int id)
        {
            var document = await _context.ClientSopDocuments.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }

            var absolutePath = Path.Combine(
                _environment.ContentRootPath,
                document.StoredFilePath.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(absolutePath))
            {
                TempData["Error"] = "The SOP file could not be found on the server.";
                return RedirectToAction(nameof(Edit), new { id = document.ClientId });
            }

            var stream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            return File(
                stream,
                document.ContentType ?? "application/pdf",
                enableRangeProcessing: true);
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
        // SOP Documents for Edit Page
        //==================================================

        private void LoadSopDocuments(int clientId)
        {
            var documents = _context.ClientSopDocuments
                .Where(s => s.ClientId == clientId)
                .OrderByDescending(s => s.Version)
                .ToList();

            ViewBag.CurrentSop = documents.FirstOrDefault(s => s.IsCurrent)
                ?? documents.FirstOrDefault();

            ViewBag.SopDocuments = documents;
        }


        private static string MakeSafeFileName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safe = new string(value
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray())
                .Trim();

            return string.IsNullOrWhiteSpace(safe) ? "Client" : safe;
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