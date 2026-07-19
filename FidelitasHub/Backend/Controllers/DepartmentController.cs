using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MasterSequenceGenerator _sequenceGenerator;

        public DepartmentController(
            ApplicationDbContext context,
            MasterSequenceGenerator sequenceGenerator)
        {
            _context = context;
            _sequenceGenerator = sequenceGenerator;
        }

        //==================================================
        // Department List / Search / Status Filter
        //==================================================

        public IActionResult Index(string searchText, string status)
        {
            var departments = _context.Departments.AsQueryable();

            //===========================
            // Search
            //===========================

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                departments = departments.Where(d =>
                    d.DepartmentCode.Contains(searchText) ||
                    d.DepartmentName.Contains(searchText) ||
                    d.Description.Contains(searchText));
            }

            //===========================
            // Status Filter
            //===========================

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active")
                {
                    departments = departments.Where(d => d.IsActive);
                }
                else if (status == "Inactive")
                {
                    departments = departments.Where(d => !d.IsActive);
                }
            }

            //===========================
            // Dashboard Statistics
            //===========================

            ViewBag.TotalDepartments = _context.Departments.Count();

            ViewBag.ActiveDepartments =
                _context.Departments.Count(d => d.IsActive);

            ViewBag.InactiveDepartments =
                _context.Departments.Count(d => !d.IsActive);

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;

            return View(departments
                .OrderBy(d => d.DepartmentCode)
                .ToList());
        }

        //==================================================
        // Add Department (GET)
        //==================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        //==================================================
        // Add Department (POST)
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Department department)
        {
            department.DepartmentCode = _sequenceGenerator.GenerateDepartmentCode();
            department.IsActive = true;
            department.CreatedDate = DateTime.Now;

            ModelState.Remove(nameof(Department.DepartmentCode));

            if (ModelState.IsValid)
            {
                bool exists = _context.Departments.Any(d =>
                    d.DepartmentName.Trim().ToLower() ==
                    department.DepartmentName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "DepartmentName",
                        "Department already exists.");

                    return View(department);
                }

                _context.Departments.Add(department);

                _context.SaveChanges();

                TempData["Success"] = "Department added successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }

        //==================================================
        // Edit Department (GET)
        //==================================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var department = _context.Departments.Find(id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        //==================================================
        // Edit Department (POST)
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Department department)
        {
            if (ModelState.IsValid)
            {
                bool exists = _context.Departments.Any(d =>
                    d.DepartmentId != department.DepartmentId &&
                    d.DepartmentName.Trim().ToLower() ==
                    department.DepartmentName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "DepartmentName",
                        "Department already exists.");

                    return View(department);
                }

                _context.Departments.Update(department);

                _context.SaveChanges();

                TempData["Success"] = "Department updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }

        //==================================================
        // Disable Department
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(int id)
        {
            try
            {
                var department = _context.Departments.Find(id);

                if (department == null)
                {
                    TempData["Error"] = "Department not found.";
                    return RedirectToAction(nameof(Index));
                }

                string departmentName = department.DepartmentName ?? "";

                int employeeCount = _context.Employees.Count(e =>
                    e.IsActive &&
                    (e.Department ?? "") == departmentName);

                if (employeeCount > 0)
                {
                    TempData["Error"] =
                        $"Cannot disable Department <b>{department.DepartmentName}</b>.<br><br>" +
                        $"It is assigned to <b>{employeeCount}</b> active employee(s).<br><br>" +
                        $"Please reassign or deactivate those employees first.";

                    return RedirectToAction(nameof(Index));
                }

                department.IsActive = false;

                _context.SaveChanges();

                TempData["Success"] = "Department disabled successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "<b>EXCEPTION:</b><br>" + ex.ToString();

                return RedirectToAction(nameof(Index));
            }
        }
        //==================================================
        // Enable Department
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(int id)
        {
            var department = _context.Departments.Find(id);

            if (department == null)
            {
                return NotFound();
            }

            department.IsActive = true;

            _context.SaveChanges();

            TempData["Success"] = "Department enabled successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}