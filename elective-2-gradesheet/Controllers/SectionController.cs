using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Services;
using Microsoft.AspNetCore.Mvc;

namespace elective_2_gradesheet.Controllers
{
    public class SectionController : Controller
    {
        private readonly ISectionService _sectionService;

        public SectionController(ISectionService sectionService)
        {
            _sectionService = sectionService;
        }

        // GET: Section
        public async Task<IActionResult> Index()
        {
            var sections = await _sectionService.GetAllSectionsAsync();
            return View(sections);
        }

        // GET: Section/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var section = await _sectionService.GetSectionWithDetailsAsync(id.Value);
                
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
                var result = await _sectionService.CreateSectionAsync(section);
                
                if (!result.success)
                {
                    ModelState.AddModelError("Name", result.message);
                    return View(section);
                }

                TempData["SuccessMessage"] = result.message;
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

            var section = await _sectionService.GetSectionByIdAsync(id.Value);
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
                var result = await _sectionService.UpdateSectionAsync(section);
                
                if (!result.success)
                {
                    ModelState.AddModelError("Name", result.message);
                    return View(section);
                }

                TempData["SuccessMessage"] = result.message;
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

            var section = await _sectionService.GetSectionWithDetailsAsync(id.Value);
                
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
            var result = await _sectionService.DeleteSectionAsync(id);
            
            if (!result.success)
            {
                TempData["ErrorMessage"] = result.message;
                return RedirectToAction(nameof(Delete), new { id });
            }

            TempData["SuccessMessage"] = result.message;
            return RedirectToAction(nameof(Index));
        }
    }
}
