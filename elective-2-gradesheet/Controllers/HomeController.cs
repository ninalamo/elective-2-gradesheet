
using elective_2_gradesheet.Models;
using elective_2_gradesheet.Services;
using Microsoft.AspNetCore.Mvc;


namespace CsvImporter.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGradeService _gradeService;
        private readonly ICsvParsingService _csvParsingService;

        public HomeController(IGradeService gradeService, ICsvParsingService csvParsingService)
        {
            _gradeService = gradeService;
            _csvParsingService = csvParsingService;
        }

        public IActionResult Index(CsvDisplayViewModel model = null)
        {
            model ??= new CsvDisplayViewModel();
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

                // 1. Pass the selected GradingPeriod from the model to the service.
                await _gradeService.ProcessAndSaveGradesAsync(records, model.GradingPeriod);

                // 2. Convert the GradingPeriod enum to a string for display.
                var tag = model.GradingPeriod.ToString().ToUpper();

                var viewModel = new CsvDisplayViewModel
                {
                    GradeRecords = records,
                    GradingPeriod = model.GradingPeriod
                };

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}
