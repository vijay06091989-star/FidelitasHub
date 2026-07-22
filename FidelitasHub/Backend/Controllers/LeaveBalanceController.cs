using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;

namespace FidelitasHub.Controllers
{
    public class LeaveBalanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Leave Balance Import
        //==================================================

        [HttpGet]
        public IActionResult Import()
        {
            return View("~/Views/LeaveBalance/Import.cshtml",
                new LeaveBalanceImportViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Import(LeaveBalanceImportViewModel model)
        {
            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            {
                TempData["Error"] = "Please select an Excel file.";

                return View("~/Views/LeaveBalance/Import.cshtml", model);
            }

            ExcelPackage.License.SetNonCommercialPersonal("Vijay Peethambaram");

            string fileName =
    Guid.NewGuid().ToString() +
    Path.GetExtension(model.ExcelFile.FileName);

            string filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "TempImports",
                fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                model.ExcelFile.CopyTo(fileStream);
            }

            HttpContext.Session.SetString(
                "LeaveImportFile",
                fileName);

            using var stream = new MemoryStream();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(stream);
            }

            stream.Position = 0;

            using var package = new ExcelPackage(stream);

            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet == null)
            {
                TempData["Error"] = "No worksheet found in the Excel file.";

                return View("~/Views/LeaveBalance/Import.cshtml", model);
            }

            int totalRows = worksheet.Dimension.Rows;

            var preview = new List<LeaveBalancePreview>();

            for (int row = 2; row <= totalRows; row++)
            {
                string employeeCode = worksheet.Cells[row, 1].Text.Trim();

                preview.Add(new LeaveBalancePreview
                {
                    EmployeeCode = employeeCode,

                    EmployeeName = worksheet.Cells[row, 2].Text.Trim(),

                    LeaveBalance = decimal.TryParse(
                        worksheet.Cells[row, 3].Text,
                        out decimal balance)
                        ? balance
                        : 0,

                    Status = _context.Employees.Any(e => e.EmployeeCode == employeeCode)
                        ? "Employee Found"
                        : "Employee Not Found"
                });
            }

            model.PreviewData = preview;

            TempData["Success"] =
                $"Excel loaded successfully. {preview.Count} record(s) found.";

            return View("~/Views/LeaveBalance/Import.cshtml", model);
        }

        //==================================================
        // Import Leave Balances
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveImport()
        {
            string? fileName =
                HttpContext.Session.GetString("LeaveImportFile");

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TempData["Error"] =
                    "Import session has expired. Please preview the Excel again.";

                return RedirectToAction(nameof(Import));
            }

            string filePath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "wwwroot",
    "TempImports",
    fileName);

            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] =
                    "Temporary import file not found.";

                return RedirectToAction(nameof(Import));
            }

            ExcelPackage.License.SetNonCommercialPersonal("Vijay Peethambaram");

            using var package = new ExcelPackage(new FileInfo(filePath));

            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet == null)
            {
                TempData["Error"] = "Worksheet not found.";

                return RedirectToAction(nameof(Import));
            }

            int totalRows = worksheet.Dimension.Rows;

            //========================================
            // Test Import - First Employee Only
            //========================================

            int imported = 0;
            int updated = 0;
            int skipped = 0;

            for (int row = 2; row <= totalRows; row++)
            {
                string employeeCode = worksheet.Cells[row, 1].Text.Trim();

                decimal leaveBalance =
                    decimal.TryParse(
                        worksheet.Cells[row, 3].Text,
                        out decimal balanceValue)
                    ? balanceValue
                    : 0;

                var employee = _context.Employees
                                       .FirstOrDefault(e => e.EmployeeCode == employeeCode);

                if (employee == null)
                {
                    skipped++;
                    continue;
                }

                var balance = _context.EmployeeLeaveBalances
                                      .FirstOrDefault(x => x.EmployeeId == employee.EmployeeId);

                if (balance == null)
                {
                    balance = new EmployeeLeaveBalance
                    {
                        EmployeeId = employee.EmployeeId,
                        CurrentLeaveBalance = leaveBalance,
                        LastUpdatedOn = DateTime.Now,
                        LastUpdatedBy = "Admin"
                    };

                    _context.EmployeeLeaveBalances.Add(balance);

                    imported++;
                }
                else
                {
                    balance.CurrentLeaveBalance = leaveBalance;
                    balance.LastUpdatedOn = DateTime.Now;
                    balance.LastUpdatedBy = "Admin";

                    updated++;
                }
            }

            _context.SaveChanges();

            //========================================
            // Release Next Payroll Leave Requests
            //========================================

            DateTime currentPayroll = GetPayrollStart(DateTime.Today);

            var queuedLeaves = _context.LeaveApplications
                .Where(x => x.Status == "Pending - Next Payroll Cycle")
                .ToList();

            foreach (var leave in queuedLeaves)
            {
                if (GetPayrollStart(leave.FromDate) == currentPayroll)
                {
                    leave.Status = "Pending";
                }
            }

            _context.SaveChanges();

            TempData["Success"] =
                $"Import Completed. Imported : {imported}, Updated : {updated}, Skipped : {skipped}";

            return RedirectToAction(nameof(Import));
        }

        //==================================================
        // Get Payroll Start Date
        //==================================================
        private DateTime GetPayrollStart(DateTime leaveDate)
        {
            if (leaveDate.Day >= 22)
            {
                return new DateTime(leaveDate.Year, leaveDate.Month, 22);
            }

            DateTime previousMonth = leaveDate.AddMonths(-1);

            return new DateTime(previousMonth.Year, previousMonth.Month, 22);
        }
    }
}