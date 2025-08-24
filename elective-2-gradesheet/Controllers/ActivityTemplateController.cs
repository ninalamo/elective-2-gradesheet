using System.Text.Json;
using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Models;
using elective_2_gradesheet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Controllers
{
    public class ActivityTemplateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityTemplateService _activityTemplateService;

        public ActivityTemplateController(ApplicationDbContext context, IActivityTemplateService activityTemplateService)
        {
            _context = context;
            _activityTemplateService = activityTemplateService;
        }

        // GET: ActivityTemplate
        public async Task<IActionResult> Index(string searchString, int? sectionId, GradingPeriod? period, string sortOrder, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["SectionSortParm"] = sortOrder == "Section" ? "section_desc" : "Section";
            ViewData["PeriodSortParm"] = sortOrder == "Period" ? "period_desc" : "Period";

            var query = _context.ActivityTemplates
                .Include(at => at.Section)
                .Where(at => at.IsActive);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(at => at.Name.Contains(searchString) || 
                                         at.Description.Contains(searchString) ||
                                         at.Tag.Contains(searchString));
            }

            // Apply section filter
            if (sectionId.HasValue)
            {
                query = query.Where(at => at.SectionId == sectionId.Value);
            }

            // Apply period filter
            if (period.HasValue)
            {
                query = query.Where(at => at.Period == period.Value);
            }

            // Apply sorting
            switch (sortOrder)
            {
                case "name_desc":
                    query = query.OrderByDescending(at => at.Name);
                    break;
                case "Section":
                    query = query.OrderBy(at => at.Section.Name);
                    break;
                case "section_desc":
                    query = query.OrderByDescending(at => at.Section.Name);
                    break;
                case "Period":
                    query = query.OrderBy(at => at.Period);
                    break;
                case "period_desc":
                    query = query.OrderByDescending(at => at.Period);
                    break;
                default:
                    query = query.OrderBy(at => at.Name);
                    break;
            }

            var activityTemplates = await query.ToListAsync();

            var sections = await _context.Sections
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();

            var viewModel = new ActivityTemplateListViewModel
            {
                ActivityTemplates = activityTemplates,
                Sections = sections,
                CurrentSearch = searchString,
                CurrentSectionId = sectionId,
                CurrentPeriod = period,
                CurrentSort = sortOrder
            };

            return View(viewModel);
        }

        // GET: ActivityTemplate/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activityTemplate = await _context.ActivityTemplates
                .Include(at => at.Section)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (activityTemplate == null)
            {
                return NotFound();
            }

            return View(activityTemplate);
        }

        // GET: ActivityTemplate/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Sections = new SelectList(await _context.Sections.Where(s => s.IsActive).ToListAsync(), "Id", "Name");
            
            var model = new ActivityTemplateCreateViewModel
            {
                MaxPoints = 100,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            return View(model);
        }

        // POST: ActivityTemplate/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityTemplateCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var activityTemplate = new ActivityTemplate
                    {
                        Name = model.Name,
                        SectionId = model.SectionId,
                        Period = model.Period,
                        MaxPoints = model.MaxPoints,
                        Tag = model.Tag,
                        Description = model.Description,
                        RubricJson = model.RubricJson,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow,
                        IsActive = model.IsActive
                    };

                    // Validate rubric JSON if provided
                    if (!string.IsNullOrEmpty(model.RubricJson))
                    {
                        var validationResult = await _activityTemplateService.ValidateRubricJsonAsync(model.RubricJson);
                        if (!validationResult.IsValid)
                        {
                            ModelState.AddModelError("RubricJson", validationResult.ErrorMessage);
                            ViewBag.Sections = new SelectList(await _context.Sections.Where(s => s.IsActive).ToListAsync(), "Id", "Name", model.SectionId);
                            return View(model);
                        }
                    }

                    _context.Add(activityTemplate);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Activity template created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating activity template: {ex.Message}");
                }
            }

            ViewBag.Sections = new SelectList(await _context.Sections.Where(s => s.IsActive).ToListAsync(), "Id", "Name", model.SectionId);
            return View(model);
        }

        // GET: ActivityTemplate/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activityTemplate = await _context.ActivityTemplates.FindAsync(id);
            if (activityTemplate == null)
            {
                return NotFound();
            }

            var model = new ActivityTemplateEditViewModel
            {
                Id = activityTemplate.Id,
                Name = activityTemplate.Name,
                SectionId = activityTemplate.SectionId,
                Period = activityTemplate.Period,
                MaxPoints = activityTemplate.MaxPoints,
                Tag = activityTemplate.Tag,
                Description = activityTemplate.Description,
                RubricJson = activityTemplate.RubricJson,
                IsActive = activityTemplate.IsActive,
                CreatedDate = activityTemplate.CreatedDate
            };

            ViewBag.Sections = new SelectList(await _context.Sections.Where(s => s.IsActive).ToListAsync(), "Id", "Name", activityTemplate.SectionId);
            return View(model);
        }

        // POST: ActivityTemplate/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActivityTemplateEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var activityTemplate = await _context.ActivityTemplates.FindAsync(id);
                    if (activityTemplate == null)
                    {
                        return NotFound();
                    }

                    // Validate rubric JSON if provided
                    if (!string.IsNullOrEmpty(model.RubricJson))
                    {
                        var validationResult = await _activityTemplateService.ValidateRubricJsonAsync(model.RubricJson);
                        if (!validationResult.IsValid)
                        {
                            ModelState.AddModelError("RubricJson", validationResult.ErrorMessage);
                            ViewBag.Sections = new SelectList(await _context.Sections.Where(s => s.IsActive).ToListAsync(), "Id", "Name", model.SectionId);
                            return View(model);
                        }
                    }

                    activityTemplate.Name = model.Name;
                    activityTemplate.SectionId = model.SectionId;
                    activityTemplate.Period = model.Period;
                    activityTemplate.MaxPoints = model.MaxPoints;
                    activityTemplate.Tag = model.Tag;
                    activityTemplate.Description = model.Description;
                    activityTemplate.RubricJson = model.RubricJson;
                    activityTemplate.IsActive = model.IsActive;
                    activityTemplate.UpdatedDate = DateTime.UtcNow;

                    _context.Update(activityTemplate);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Activity template updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActivityTemplateExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating activity template: {ex.Message}");
                }
            }

            ViewBag.Sections = new SelectList(await _context.Sections.Where(s => s.IsActive).ToListAsync(), "Id", "Name", model.SectionId);
            return View(model);
        }

        // GET: ActivityTemplate/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activityTemplate = await _context.ActivityTemplates
                .Include(at => at.Section)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (activityTemplate == null)
            {
                return NotFound();
            }

            return View(activityTemplate);
        }

        // POST: ActivityTemplate/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activityTemplate = await _context.ActivityTemplates.FindAsync(id);
            if (activityTemplate != null)
            {
                // Soft delete - mark as inactive instead of deleting
                activityTemplate.IsActive = false;
                activityTemplate.UpdatedDate = DateTime.UtcNow;
                _context.Update(activityTemplate);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Activity template deactivated successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: ActivityTemplate/Duplicate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(int id)
        {
            var originalTemplate = await _context.ActivityTemplates.FindAsync(id);
            if (originalTemplate == null)
            {
                return NotFound();
            }

            var duplicatedTemplate = new ActivityTemplate
            {
                Name = $"{originalTemplate.Name} (Copy)",
                SectionId = originalTemplate.SectionId,
                Period = originalTemplate.Period,
                MaxPoints = originalTemplate.MaxPoints,
                Tag = originalTemplate.Tag,
                Description = originalTemplate.Description,
                RubricJson = originalTemplate.RubricJson,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.Add(duplicatedTemplate);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Activity template duplicated successfully!";
            return RedirectToAction(nameof(Edit), new { id = duplicatedTemplate.Id });
        }

        // GET: ActivityTemplate/RubricEditor/5
        public async Task<IActionResult> RubricEditor(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activityTemplate = await _context.ActivityTemplates
                .Include(at => at.Section)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (activityTemplate == null)
            {
                return NotFound();
            }

            var model = new RubricEditorViewModel
            {
                ActivityTemplateId = activityTemplate.Id,
                ActivityTemplateName = activityTemplate.Name,
                RubricJson = activityTemplate.RubricJson ?? "[]"
            };

            return View(model);
        }

        // POST: ActivityTemplate/SaveRubric
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRubric(RubricEditorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var activityTemplate = await _context.ActivityTemplates.FindAsync(model.ActivityTemplateId);
                if (activityTemplate == null)
                {
                    return NotFound();
                }

                // Validate rubric JSON
                var validationResult = await _activityTemplateService.ValidateRubricJsonAsync(model.RubricJson);
                if (!validationResult.IsValid)
                {
                    ModelState.AddModelError("RubricJson", validationResult.ErrorMessage);
                    return View("RubricEditor", model);
                }

                activityTemplate.RubricJson = model.RubricJson;
                activityTemplate.UpdatedDate = DateTime.UtcNow;

                _context.Update(activityTemplate);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Rubric saved successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View("RubricEditor", model);
        }

        // POST: ActivityTemplate/ValidateRubric
        [HttpPost]
        public async Task<IActionResult> ValidateRubric([FromBody] RubricValidationRequest request)
        {
            var validationResult = await _activityTemplateService.ValidateRubricJsonAsync(request.RubricJson);
            
            return Json(new 
            { 
                success = validationResult.IsValid, 
                message = validationResult.ErrorMessage,
                totalPoints = validationResult.TotalPoints
            });
        }

        // GET: ActivityTemplate/GetSampleRubric
        [HttpGet]
        public async Task<IActionResult> GetSampleRubric()
        {
            var sampleRubricJson = await _activityTemplateService.GetSampleRubricJsonAsync();
            return Content(sampleRubricJson, "application/json");
        }

        private bool ActivityTemplateExists(int id)
        {
            return _context.ActivityTemplates.Any(e => e.Id == id);
        }
    }

    public class RubricValidationRequest
    {
        public string RubricJson { get; set; } = string.Empty;
    }
}
