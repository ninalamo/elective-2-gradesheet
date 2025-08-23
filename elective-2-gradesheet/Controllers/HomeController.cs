
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
        private readonly IGitHubRepositoryService _gitHubRepositoryService;
        private readonly ApplicationDbContext _context;

        public HomeController(IGradeService gradeService, ICsvParsingService csvParsingService, IGitHubRepositoryService gitHubRepositoryService, ApplicationDbContext dbContext)
        {
            _gradeService = gradeService;
            _csvParsingService = csvParsingService;
            _gitHubRepositoryService = gitHubRepositoryService;
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

            // Call the correct service method to get student groups
            var studentGroups = await _gradeService.GetStudentGroupsAsync(searchString, sectionId, period, sortOrder, pageNumber ?? 1, 10);

            // Get the list of sections to populate the filter dropdown
            var sections = await _gradeService.GetActiveSectionsAsync();

            // Create the ViewModel that holds all the data for the view
            var viewModel = new RecordsViewModel
            {
                StudentGroups = studentGroups, // Assign the result directly
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
        public async Task<IActionResult> UpdateActivity( int studentId, double points, double maxPoints, GradingPeriod period, string tag, string otherTag, string githubLink, string status, int? activityId = default, string activityName = "", int? newId = 0)
        {
            // The UpdateActivityAsync method in the service will handle the logic
            await _gradeService.UpdateActivityAsync( studentId, points, maxPoints, period, tag, otherTag, githubLink, status, activityId, activityName, newId);
            return RedirectToAction("StudentProfile", new { id = studentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Ensure anti-forgery token is validated
        public async Task<IActionResult> BulkAddMissingActivities(int studentId, GradingPeriod gradingPeriod)
        {
            try
            {
                var addedCount = await _gradeService.BulkAddMissingActivitiesAsync(studentId, gradingPeriod);

                if (addedCount > 0)
                {
                    // Return JSON for success with count
                    return Json(new { success = true, message = $"Successfully added {addedCount} missing activities for {gradingPeriod.ToString()}!", type = "success", addedCount = addedCount });
                }
                else
                {
                    // Return JSON for no activities found (changed to 'warning' type)
                    return Json(new { success = true, message = $"No missing activities found for {gradingPeriod.ToString()} to add.", type = "warning", addedCount = addedCount });
                }
            }
            catch (Exception ex)
            {
                // Return JSON for error
                return Json(new { success = false, message = $"Error adding missing activities: {ex.Message}", type = "danger", addedCount = 0 });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloneGitHubRepository(string githubUrl, string destinationPath, string keywords, bool cleanupAfterScan = false)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(githubUrl))
                {
                    return Json(new { success = false, message = "Please provide a GitHub URL.", type = "danger" });
                }

                // Set default destination path if not provided
                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    destinationPath = @"D:\temporary_github";
                }

                // Parse keywords if provided
                var keywordList = new List<string>();
                if (!string.IsNullOrWhiteSpace(keywords))
                {
                    keywordList = keywords.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(k => k.Trim())
                                         .Where(k => !string.IsNullOrWhiteSpace(k))
                                         .ToList();
                }

                // Clone and scan the repository
                var result = await _gitHubRepositoryService.CloneAndScanRepositoryAsync(githubUrl, destinationPath, keywordList);

                if (result.Success)
                {
                    var responseData = new
                    {
                        success = true,
                        message = result.Message,
                        type = "success",
                        clonedPath = result.ClonedPath,
                        foundKeywords = result.FoundKeywords,
                        scannedFiles = result.ScannedFiles.Select(f => new
                        {
                            fileName = f.FileName,
                            filePath = f.FilePath,
                            matchedKeywords = f.MatchedKeywords,
                            matchedLines = f.MatchedLines.Take(5).ToList() // Limit to first 5 matches per file
                        }).ToList()
                    };

                    // Cleanup if requested
                    if (cleanupAfterScan && !string.IsNullOrWhiteSpace(result.ClonedPath))
                    {
                        await _gitHubRepositoryService.CleanupRepositoryAsync(result.ClonedPath);
                    }

                    return Json(responseData);
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.Message,
                        type = "danger",
                        errorDetails = result.ErrorDetails
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while processing the GitHub repository.",
                    type = "danger",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanWithRubric(string githubUrl, string destinationPath, string rubricJson, bool cleanupAfterScan, int studentId, int activityId)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(githubUrl))
                {
                    return Json(new { success = false, message = "Please provide a GitHub URL.", type = "danger" });
                }

                if (string.IsNullOrWhiteSpace(rubricJson))
                {
                    return Json(new { success = false, message = "Please provide a rubric JSON.", type = "danger" });
                }

                // Set default destination path if not provided
                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    destinationPath = @"D:\temporary_github";
                }

                // Parse and validate rubric JSON
                List<SimpleRubricItem> rubric;
                try
                {
                    rubric = SimpleRubricHelper.DeserializeRubric(rubricJson);
                    if (rubric == null)
                    {
                        return Json(new { success = false, message = "Failed to parse rubric JSON.", type = "danger" });
                    }

                    // Validate rubric structure
                    foreach (var item in rubric)
                    {
                        if (string.IsNullOrWhiteSpace(item.Title) || item.Score <= 0 || 
                            item.Files == null || !item.Files.Any() || 
                            item.Keywords == null || !item.Keywords.Any())
                        {
                            return Json(new { success = false, message = "Invalid rubric structure. Each item must have: title, score > 0, files array, and keywords array.", type = "danger" });
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"Invalid JSON format: {ex.Message}", type = "danger" });
                }

                // Scan with rubric
                var result = await _gitHubRepositoryService.CloneAndScanWithRubricAsync(githubUrl, destinationPath, rubric, studentId, activityId);

                if (result != null)
                {
                    var responseData = new
                    {
                        success = true,
                        message = $"Repository scanned successfully! Total score: {result.TotalScore}/{result.MaxPossibleScore} ({result.Percentage:F1}%)",
                        type = "success",
                        data = new
                        {
                            studentId = result.StudentId,
                            activityId = result.ActivityId,
                            repositoryUrl = result.RepositoryUrl,
                            scannedDate = result.ScannedDate,
                            totalScore = result.TotalScore,
                            maxPossibleScore = result.MaxPossibleScore,
                            percentage = result.Percentage,
                            rubricItems = result.RubricItems.Select(item => new
                            {
                                title = item.Title,
                                maxScore = item.MaxScore,
                                earnedScore = item.EarnedScore,
                                foundFiles = item.FoundFiles,
                                missingFiles = item.MissingFiles,
                                foundKeywords = item.FoundKeywords,
                                missingKeywords = item.MissingKeywords,
                                keywordMatches = item.KeywordMatches.Select(match => new
                                {
                                    keyword = match.Keyword,
                                    lineNumber = match.LineNumber,
                                    line = match.Line,
                                    context = match.Context
                                }).ToList()
                            }).ToList()
                        }
                    };

                    // Cleanup if requested and we have a valid path
                    if (cleanupAfterScan)
                    {
                        // The service should have cleaned up automatically, but we can add extra cleanup here if needed
                        // For now, we'll trust the service to handle cleanup
                    }

                    return Json(responseData);
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = "Failed to scan repository with rubric. No results returned.",
                        type = "danger"
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while scanning the repository with the rubric.",
                    type = "danger",
                    errorDetails = ex.Message
                });
            }
        }

    }
}
