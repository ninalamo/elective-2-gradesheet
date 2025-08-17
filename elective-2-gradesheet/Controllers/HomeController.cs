
using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Models;
using elective_2_gradesheet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace CsvImporter.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGradeService _gradeService;
        private readonly ICsvParsingService _csvParsingService;
        private readonly ApplicationDbContext _context;

        public HomeController(IGradeService gradeService, ICsvParsingService csvParsingService, ApplicationDbContext dbContext)
        {
            _gradeService = gradeService;
            _csvParsingService = csvParsingService;
            _context = dbContext;
        }

        public IActionResult Index(CsvDisplayViewModel model = null)
        {
            model ??= new CsvDisplayViewModel();
            ViewBag.Sections = _context.Sections
                .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                .ToList();
            return View(model);
        }

        [HttpPost]
        // The signature is changed to accept the full CsvDisplayViewModel from the form.
        // This model will contain the selected GradingPeriod.
        public async Task<IActionResult> Upload(IFormFile file, CsvDisplayViewModel model)
        {
            ViewBag.Sections = _context.Sections
              .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
              .ToList();

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction("Index");
            }

            try
            {
                var records = _csvParsingService.ParseGradesCsv(file).ToList();

                model.GradeRecords = records;
                

                // 1. Pass the selected GradingPeriod from the model to the service.
                await _gradeService.ProcessAndSaveGradesAsync(model);

                // 2. Convert the GradingPeriod enum to a string for display.
                var tag = model.GradingPeriod.ToString().ToUpper();

               

                return View("Index", model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex?.InnerException?.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Records(string searchString, int? sectionId, GradingPeriod? period, string sortOrder, int? pageNumber)
        {
            // Set up ViewData for the sorting links in the table header
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["SectionSortParm"] = sortOrder == "Section" ? "section_desc" : "Section";
            ViewData["Sections"] = _context.Sections.AsNoTracking().Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() }).ToArray();

            // Call the service with all the filter and sort parameters
            var activities = await _gradeService.GetActivitiesAsync(searchString, sectionId, period, sortOrder, pageNumber ?? 1, 10);

            // Get the list of sections to populate the filter dropdown
            var sections = await _gradeService.GetActiveSectionsAsync();

            // Create the ViewModel that holds all the data for the view
            var viewModel = new RecordsViewModel
            {
                Activities = activities,
                Sections = sections.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }),
                CurrentSearch = searchString,
                CurrentSectionId = sectionId,
                CurrentPeriod = period,
                CurrentSort = sortOrder
            };

            return View(viewModel);
        }

        public async Task<IActionResult> StudentProfile(int id, GradingPeriod? period, string sortOrder)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["PeriodSortParm"] = string.IsNullOrEmpty(sortOrder) ? "period_desc" : "";

            var viewModel = await _gradeService.GetStudentProfileAsync(id, period, sortOrder);

            if (viewModel == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }
    }
}
