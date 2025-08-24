using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Controllers
{
    public class SectionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Section
        public async Task<IActionResult> Index()
        {
            var sections = await _context.Sections
                .Include(s => s.Students)
                .Include(s => s.ActivityTemplates)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(sections);
        }

        // GET: Section/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.Sections
                .Include(s => s.Students)
                .Include(s => s.ActivityTemplates)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (section == null)
            {
                return NotFound();
            }

            return View(section);
        }

        // GET: Section/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Section/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,SchoolYear,IsActive")] Section section)
        {
            if (ModelState.IsValid)
            {
                // Check if section name already exists for the same school year
                var existingSection = await _context.Sections
                    .FirstOrDefaultAsync(s => s.Name == section.Name && s.SchoolYear == section.SchoolYear);
                
                if (existingSection != null)
                {
                    ModelState.AddModelError("Name", "A section with this name already exists for the selected school year.");
                    return View(section);
                }

                _context.Add(section);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Section '{section.Name}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(section);
        }

        // GET: Section/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.Sections.FindAsync(id);
            if (section == null)
            {
                return NotFound();
            }
            return View(section);
        }

        // POST: Section/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,SchoolYear,IsActive")] Section section)
        {
            if (id != section.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if section name already exists for the same school year (excluding current section)
                    var existingSection = await _context.Sections
                        .FirstOrDefaultAsync(s => s.Name == section.Name && s.SchoolYear == section.SchoolYear && s.Id != id);
                    
                    if (existingSection != null)
                    {
                        ModelState.AddModelError("Name", "A section with this name already exists for the selected school year.");
                        return View(section);
                    }

                    _context.Update(section);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Section '{section.Name}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SectionExists(section.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(section);
        }

        // GET: Section/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _context.Sections
                .Include(s => s.Students)
                .Include(s => s.ActivityTemplates)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (section == null)
            {
                return NotFound();
            }

            return View(section);
        }

        // POST: Section/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var section = await _context.Sections
                .Include(s => s.Students)
                .Include(s => s.ActivityTemplates)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (section != null)
            {
                // Check if section has students or activity templates
                if (section.Students.Any() || section.ActivityTemplates.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete section that contains students or activity templates. Please move or delete them first.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Sections.Remove(section);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Section '{section.Name}' deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SectionExists(int id)
        {
            return _context.Sections.Any(e => e.Id == id);
        }
    }
}
