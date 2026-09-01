using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using FidelitasHub.Services.Leave;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace FidelitasHub.Controllers
{
    public class LeaveBalanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PayrollCycleLeaveService _payrollCycleLeaveService;

        public LeaveBalanceController(
            ApplicationDbContext context,
            PayrollCycleLeaveService payrollCycleLeaveService)
        {
            _context = context;
            _payrollCycleLeaveService = payrollCycleLeaveService;
        }

        //==================================================
        // Leave Balance Import Page
        //==================================================

        [HttpGet]
        public IActionResult Import()
        {
            var model = new LeaveBalanceImportViewModel
            {
                PayrollCycles = _context.PayrollCalendars
                    .OrderByDescending(x => x.PeriodStart)
                    .ToList()
            };

            return View(
                "~/Views/LeaveBalance/Import.cshtml",
                model);
        }

        //==================================================
        // Preview Excel File
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Import(
            LeaveBalanceImportViewModel model)
        {
            model.PayrollCycles = _context.PayrollCalendars
                .OrderByDescending(x => x.PeriodStart)
                .ToList();

            //==================================================
            // Validate Payroll Cycle
            //==================================================

            if (model.PayrollCalendarId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.PayrollCalendarId),
                    "Please select a payroll cycle.");

                return View(
                    "~/Views/LeaveBalance/Import.cshtml",
                    model);
            }

            //==================================================
            // Validate Excel File
            //==================================================

            if (model.ExcelFile == null ||
                model.ExcelFile.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.ExcelFile),
                    "Please select an Excel file.");

                return View(
                    "~/Views/LeaveBalance/Import.cshtml",
                    model);
            }

            //==================================================
            // Get Selected Payroll Cycle
            //==================================================

            var payroll = _context.PayrollCalendars
                .FirstOrDefault(x =>
                    x.PayrollCalendarId ==
                    model.PayrollCalendarId);

            if (payroll == null)
            {
                ModelState.AddModelError(
                    nameof(model.PayrollCalendarId),
                    "Invalid payroll cycle selected.");

                return View(
                    "~/Views/LeaveBalance/Import.cshtml",
                    model);
            }

            //==================================================
            // EPPlus License
            //==================================================

            ExcelPackage.License.SetNonCommercialPersonal(
                "Vijay Peethambaram");

            //==================================================
            // Create Temporary File
            //==================================================

            string fileName =
                Guid.NewGuid().ToString() +
                Path.GetExtension(model.ExcelFile.FileName);

            string tempDirectory =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "TempImports");

            Directory.CreateDirectory(tempDirectory);

            string filePath =
                Path.Combine(
                    tempDirectory,
                    fileName);

            using (var fileStream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                model.ExcelFile.CopyTo(fileStream);
            }

            //==================================================
            // Store File Name
            //==================================================

            HttpContext.Session.SetString(
                "LeaveImportFile",
                fileName);

            // Store selected payroll cycle as well
            HttpContext.Session.SetInt32(
                "LeaveImportPayrollCalendarId",
                model.PayrollCalendarId);

            //==================================================
            // Read Excel File
            //==================================================

            using var stream = new MemoryStream();

            using (var fileStream =
                   new FileStream(
                       filePath,
                       FileMode.Open,
                       FileAccess.Read))
            {
                fileStream.CopyTo(stream);
            }

            stream.Position = 0;

            using var package =
                new ExcelPackage(stream);

            if (package.Workbook.Worksheets.Count == 0)
            {
                TempData["Error"] =
                    "No worksheet found in the Excel file.";

                return View(
                    "~/Views/LeaveBalance/Import.cshtml",
                    model);
            }

            var worksheet =
                package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
            {
                TempData["Error"] =
                    "The Excel worksheet is empty.";

                return View(
                    "~/Views/LeaveBalance/Import.cshtml",
                    model);
            }

            int totalRows =
                worksheet.Dimension.Rows;

            var preview =
                new List<LeaveBalancePreview>();

            //==================================================
            // Build Preview
            //==================================================

            for (int row = 2;
                 row <= totalRows;
                 row++)
            {
                string employeeCode =
                    worksheet.Cells[row, 1]
                        .Text
                        .Trim();

                // Ignore blank rows
                if (string.IsNullOrWhiteSpace(
                    employeeCode))
                {
                    continue;
                }

                string employeeName =
                    worksheet.Cells[row, 2]
                        .Text
                        .Trim();

                decimal leaveBalance =
                    decimal.TryParse(
                        worksheet.Cells[row, 3].Text,
                        out decimal balance)
                    ? balance
                    : 0;

                bool employeeExists =
                    _context.Employees.Any(e =>
                        e.EmployeeCode ==
                        employeeCode);

                preview.Add(
                    new LeaveBalancePreview
                    {
                        EmployeeCode = employeeCode,
                        EmployeeName = employeeName,
                        LeaveBalance = leaveBalance,

                        Status = employeeExists
                            ? "Employee Found"
                            : "Employee Not Found"
                    });
            }

            model.PreviewData = preview;

            TempData["Success"] =
                $"Excel loaded successfully. " +
                $"{preview.Count} record(s) found.";

            return View(
                "~/Views/LeaveBalance/Import.cshtml",
                model);
        }

        //==================================================
        // Save Leave Balance Import
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveImport(
            LeaveBalanceImportViewModel model)
        {
            //==================================================
            // Get Payroll Cycle
            //==================================================

            int payrollCalendarId =
                model.PayrollCalendarId;

            if (payrollCalendarId <= 0)
            {
                payrollCalendarId =
                    HttpContext.Session.GetInt32(
                        "LeaveImportPayrollCalendarId")
                    ?? 0;
            }

            if (payrollCalendarId <= 0)
            {
                TempData["Error"] =
                    "Please select a payroll cycle.";

                return RedirectToAction(
                    nameof(Import));
            }

            //==================================================
            // Get Selected Payroll Cycle
            //==================================================

            var payroll =
                await _context.PayrollCalendars
                    .FirstOrDefaultAsync(x =>
                        x.PayrollCalendarId ==
                        payrollCalendarId);

            if (payroll == null)
            {
                TempData["Error"] =
                    "Invalid payroll cycle selected.";

                return RedirectToAction(
                    nameof(Import));
            }

            //==================================================
            // Payroll Dates
            //==================================================

            DateTime payrollStart =
                payroll.PeriodStart.Date;

            DateTime payrollEnd =
                payroll.PeriodEnd.Date;

            //==================================================
            // Get Temporary Excel File
            //==================================================

            string? fileName =
                HttpContext.Session.GetString(
                    "LeaveImportFile");

            if (string.IsNullOrWhiteSpace(
                fileName))
            {
                TempData["Error"] =
                    "Import session expired. " +
                    "Please upload and preview the Excel file again.";

                return RedirectToAction(
                    nameof(Import));
            }

            string tempDirectory =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "TempImports");

            string filePath =
                Path.Combine(
                    tempDirectory,
                    fileName);

            if (!System.IO.File.Exists(
                filePath))
            {
                TempData["Error"] =
                    "Temporary Excel file was not found. " +
                    "Please upload the file again.";

                HttpContext.Session.Remove(
                    "LeaveImportFile");

                HttpContext.Session.Remove(
                    "LeaveImportPayrollCalendarId");

                return RedirectToAction(
                    nameof(Import));
            }

            //==================================================
            // Read Excel File
            //==================================================

            ExcelPackage.License.SetNonCommercialPersonal(
                "Vijay Peethambaram");

            using var stream =
                new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read);

            using var package =
                new ExcelPackage(stream);

            if (package.Workbook.Worksheets.Count == 0)
            {
                TempData["Error"] =
                    "No worksheet found in the Excel file.";

                return RedirectToAction(
                    nameof(Import));
            }

            var worksheet =
                package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
            {
                TempData["Error"] =
                    "The Excel worksheet is empty.";

                return RedirectToAction(
                    nameof(Import));
            }

            int totalRows =
                worksheet.Dimension.Rows;

            int imported = 0;
            int updated = 0;
            int skipped = 0;

            //==================================================
            // Process Excel Rows
            //==================================================

            for (int row = 2;
                 row <= totalRows;
                 row++)
            {
                string employeeCode =
                    worksheet.Cells[row, 1]
                        .Text
                        .Trim();

                if (string.IsNullOrWhiteSpace(
                    employeeCode))
                {
                    continue;
                }

                decimal leaveBalance =
                    decimal.TryParse(
                        worksheet.Cells[row, 3].Text,
                        out decimal balanceValue)
                    ? balanceValue
                    : 0;

                //==================================================
                // Find Employee
                //==================================================

                var employee =
                    await _context.Employees
                        .FirstOrDefaultAsync(e =>
                            e.EmployeeCode ==
                            employeeCode);

                if (employee == null)
                {
                    skipped++;
                    continue;
                }

                //==================================================
                // Find Leave Balance
                //
                // ONLY for the selected payroll cycle.
                //==================================================

                var balance =
                    await _context.EmployeeLeaveBalances
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId ==
                            employee.EmployeeId &&

                            x.BalancePeriodStart.HasValue &&
                            x.BalancePeriodEnd.HasValue &&

                            x.BalancePeriodStart.Value.Date ==
                            payrollStart &&

                            x.BalancePeriodEnd.Value.Date ==
                            payrollEnd);

                //==================================================
                // Create New Balance
                //==================================================

                if (balance == null)
                {
                    balance =
                        new EmployeeLeaveBalance
                        {
                            EmployeeId =
                                employee.EmployeeId,

                            CurrentLeaveBalance =
                                leaveBalance,

                            BalancePeriodStart =
                                payrollStart,

                            BalancePeriodEnd =
                                payrollEnd,

                            LastUpdatedOn =
                                DateTimeHelper.GetIST(),

                            LastUpdatedBy =
                                "Admin"
                        };

                    _context.EmployeeLeaveBalances
                        .Add(balance);

                    imported++;
                }

                //==================================================
                // Update Existing Balance
                //==================================================

                else
                {
                    balance.CurrentLeaveBalance =
                        leaveBalance;

                    balance.BalancePeriodStart =
                        payrollStart;

                    balance.BalancePeriodEnd =
                        payrollEnd;

                    balance.LastUpdatedOn =
                        DateTimeHelper.GetIST();

                    balance.LastUpdatedBy =
                        "Admin";

                    updated++;
                }
            }

            //==================================================
            // Save all balance changes
            //==================================================

            await _context.SaveChangesAsync();

            //==================================================
            // Mark selected payroll cycle as Leave Credit Done
            //==================================================

            payroll.IsLeaveCreditProcessed = true;

            _context.PayrollCalendars.Update(payroll);

            await _context.SaveChangesAsync();

            //==================================================
            // Synchronize leave applications
            //==================================================

            await _payrollCycleLeaveService
                .SynchronizeAsync(payroll);

            //==================================================
            // Import Result Message
            //==================================================

            TempData["Success"] =
                $"Import completed for " +
                $"{payrollStart:dd-MMM-yyyy} to " +
                $"{payrollEnd:dd-MMM-yyyy}. " +
                $"Imported: {imported}, " +
                $"Updated: {updated}, " +
                $"Skipped: {skipped}";

            //==================================================
            // Delete Temporary File
            //==================================================

            try
            {
                System.IO.File.Delete(
                    filePath);
            }
            catch
            {
                // File cleanup failure is not fatal
            }

            //==================================================
            // Clear Session
            //==================================================

            HttpContext.Session.Remove(
                "LeaveImportFile");

            HttpContext.Session.Remove(
                "LeaveImportPayrollCalendarId");

            return RedirectToAction(
                nameof(Import));
        }
    }
}