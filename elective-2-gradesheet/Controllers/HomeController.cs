using System.Text.Json;
using System.Text.RegularExpressions;
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

        public IActionResult Privacy() => View();

        [HttpPost]
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
                await _gradeService.ProcessAndSaveGradesAsync(model);
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
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["SectionSortParm"] = sortOrder == "Section" ? "section_desc" : "Section";
            var studentGroups = await _gradeService.GetStudentGroupsAsync(searchString, sectionId, period, sortOrder, pageNumber ?? 1, 10);
            var sections = await _gradeService.GetActiveSectionsAsync();
            var viewModel = new RecordsViewModel
            {
                StudentGroups = studentGroups,
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

        [HttpPost]
        public async Task<IActionResult> UpdateActivity( int studentId, double points, double maxPoints, GradingPeriod period, string? tag = "", string? otherTag = "", string? githubLink = "", string? status = "Added", int? activityId = default, string activityName = "", int? newId = 0)
        {
            await _gradeService.UpdateActivityAsync( studentId, points, maxPoints, period, tag, otherTag, githubLink, status, activityId, activityName, newId);
            return RedirectToAction("StudentProfile", new { id = studentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAddMissingActivities(int studentId, GradingPeriod gradingPeriod)
        {
            try
            {
                var addedCount = await _gradeService.BulkAddMissingActivitiesAsync(studentId, gradingPeriod);
                if (addedCount > 0)
                {
                    return Json(new { success = true, message = $"Successfully added {addedCount} missing activities for {gradingPeriod.ToString()}!", type = "success", addedCount });
                }
                else
                {
                    return Json(new { success = true, message = $"No missing activities found for {gradingPeriod.ToString()} to add.", type = "warning", addedCount });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error adding missing activities: {ex.Message}", type = "danger", addedCount = 0 });
            }
        }

       [HttpPost]
        public async Task<IActionResult> ScoreActivity([FromForm] IFormFileCollection files, [FromForm] string rubricJson, [FromForm] string[] filePaths)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "Please upload files to score." });
            }

            if (string.IsNullOrEmpty(rubricJson))
            {
                return BadRequest(new { message = "Please provide a rubric." });
            }

            try
            {
                var rubric = JsonSerializer.Deserialize<List<RubricItem>>(rubricJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var scoringResults = new List<ScoringResult>();
                var totalScore = 0;
                var fileContents = new List<FileContent>();
                
                for (int i = 0; i < files.Count; i++)
                {
                    using (var reader = new StreamReader(files[i].OpenReadStream()))
                    {
                        fileContents.Add(new FileContent { Name = files[i].FileName, Path = filePaths[i], Content = await reader.ReadToEndAsync() });
                    }
                }

                foreach (var item in rubric)
                {
                    var criterionMet = false;
                    foreach (var filePattern in item.Files)
                    {
                        var regex = new Regex(WildcardToRegex(filePattern));
                        var relevantFiles = fileContents.Where(f => !string.IsNullOrEmpty(f.Path) && regex.IsMatch(f.Path.Replace("\\", "/"))).ToList();
                        
                        foreach (var file in relevantFiles)
                        {
                            string proof = "";
                            var allKeywordsFound = item.Keywords.All(keyword => {
                                var found = file.Content.Contains(keyword);
                                if (found && string.IsNullOrEmpty(proof))
                                {
                                    proof = GetLineWithKeyword(file.Content, keyword);
                                }
                                return found;
                            });

                            if (allKeywordsFound)
                            {
                                totalScore += item.Points;
                                scoringResults.Add(new ScoringResult { FileName = file.Name, Criterion = item.Name, Points = item.Points, Proof = proof });
                                criterionMet = true;
                                break; 
                            }
                        }
                        if (criterionMet) break;
                    }
                }

                return Ok(new { totalScore, results = scoringResults });
            }
            catch (JsonException)
            {
                return BadRequest(new { message = "Invalid JSON format in rubric." });
            }
        }

        private string GetLineWithKeyword(string content, string keyword)
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return lines.FirstOrDefault(line => line.Contains(keyword))?.Trim() ?? "";
        }

        public static string WildcardToRegex(string pattern)
        {
            return Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }
    }

    public class RubricItem
    {
        public string Name { get; set; }
        public int Points { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> Files { get; set; }
    }

    public class FileContent
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Content { get; set; }
    }

    public class ScoringResult
    {
        public string FileName { get; set; }
        public string Criterion { get; set; }
        public int Points { get; set; }
        public string Proof { get; set; }
    }
}