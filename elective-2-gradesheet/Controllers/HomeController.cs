
using elective_2_gradesheet.Data;
using elective_2_gradesheet.Models;
using elective_2_gradesheet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;


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

        public async Task<IActionResult> Records(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            // Fetch the paginated list of activities from the service
            var activities = await _gradeService.GetActivitiesAsync(searchString, pageNumber ?? 1, 10); // 10 records per page

            return View(activities);
        }
    }
}
