using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class DesignationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MasterSequenceGenerator _sequenceGenerator;

        public DesignationController(
            ApplicationDbContext context,
            MasterSequenceGenerator sequenceGenerator)
        {
            _context = context;
            _sequenceGenerator = sequenceGenerator;
        }

        public IActionResult Index(string searchText, string status)
        {
            var designations = _context.Designations.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                designations = designations.Where(d =>
                    d.DesignationCode.Contains(searchText) ||
                    d.DesignationName.Contains(searchText) ||
                    d.Description.Contains(searchText));
            }

            if (status == "Active")
            {
                designations = designations.Where(d => d.IsActive);
            }
            else if (status == "Inactive")
            {
                designations = designations.Where(d => !d.IsActive);
            }

            ViewBag.TotalDesignations = _context.Designations.Count();
            ViewBag.ActiveDesignations = _context.Designations.Count(d => d.IsActive);
            ViewBag.InactiveDesignations = _context.Designations.Count(d => !d.IsActive);
            ViewBag.SearchText = searchText;
            ViewBag.Status = status;

            return View(designations.OrderBy(d => d.DesignationCode).ToList());
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Designation designation)
        {
            designation.DesignationCode = _sequenceGenerator.GenerateDesignationCode();
            designation.IsActive = true;
            designation.CreatedDate = DateTime.Now;

            ModelState.Remove(nameof(Designation.DesignationCode));

            if (ModelState.IsValid)
            {
                bool exists = _context.Designations.Any(d =>
                    d.DesignationName.Trim().ToLower() ==
                    designation.DesignationName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError("DesignationName", "Designation already exists.");
                    return View(designation);
                }

                _context.Designations.Add(designation);
                _context.SaveChanges();

                TempData["Success"] = "Designation added successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(designation);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var designation = _context.Designations.Find(id);
            if (designation == null) return NotFound();
            return View(designation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Designation designation)
        {
            if (ModelState.IsValid)
            {
                bool exists = _context.Designations.Any(d =>
                    d.DesignationId != designation.DesignationId &&
                    d.DesignationName.Trim().ToLower() ==
                    designation.DesignationName.Trim().ToLower());

                if (exists)
                {
                    ModelState.AddModelError("DesignationName", "Designation already exists.");
                    return View(designation);
                }

                designation.ModifiedDate = DateTime.Now;
                _context.Designations.Update(designation);
                _context.SaveChanges();

                TempData["Success"] = "Designation updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(designation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(int id)
        {
            var designation = _context.Designations.Find(id);
            if (designation == null) return NotFound();

            designation.IsActive = false;
            _context.SaveChanges();

            TempData["Success"] = "Designation disabled successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(int id)
        {
            var designation = _context.Designations.Find(id);
            if (designation == null) return NotFound();

            designation.IsActive = true;
            _context.SaveChanges();

            TempData["Success"] = "Designation enabled successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
