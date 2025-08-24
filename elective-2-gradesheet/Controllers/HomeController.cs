using System.Text.Json;
using System.Text.Json.Serialization;
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
        private readonly IGitService _gitService;

        public HomeController(IGradeService gradeService, ICsvParsingService csvParsingService, ApplicationDbContext dbContext, IGitService gitService)
        {
            _gradeService = gradeService;
            _csvParsingService = csvParsingService;
            _context = dbContext;
            _gitService = gitService;
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
        public async Task<IActionResult> UpdateActivity(int studentId, double points, double maxPoints, GradingPeriod period, string? tag = "", string? otherTag = "", string? githubLink = "", string? status = "Added", int? activityId = default, string activityName = "", int? newId = 0)
        {
            await _gradeService.UpdateActivityAsync(studentId, points, maxPoints, period, tag, otherTag, githubLink, status, activityId, activityName, newId);
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
                    var proof = "N/A";
                    var fileName = "N/A";

                    foreach (var filePattern in item.Files)
                    {
                        var regex = new Regex(WildcardToRegex(filePattern));
                        var relevantFiles = fileContents.Where(f => !string.IsNullOrEmpty(f.Path) && regex.IsMatch(f.Path.Replace("\\", "/"))).ToList();

                        foreach (var file in relevantFiles)
                        {
                            var normalizedFileContent = Regex.Replace(file.Content, @"\s+", "").ToLower();
                            var allKeywordsFound = item.Keywords.All(keyword => {
                                var normalizedKeyword = Regex.Replace(keyword, @"\s+", "").ToLower();
                                return normalizedFileContent.Contains(normalizedKeyword);
                            });

                            if (allKeywordsFound)
                            {
                                totalScore += item.Points;
                                proof = GetLineWithKeyword(file.Content, item.Keywords.First());
                                fileName = file.Name;
                                criterionMet = true;
                                break;
                            }
                        }
                        if (criterionMet) break;
                    }
                    scoringResults.Add(new ScoringResult { FileName = fileName, Criterion = item.Name, Points = criterionMet ? item.Points : 0, Proof = proof, Met = criterionMet });
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
            return lines.FirstOrDefault(line => line.ToLower().Contains(keyword.ToLower()))?.Trim() ?? "";
        }

        [HttpPost]
        public async Task<IActionResult> CloneRepository([FromBody] CloneRepositoryRequest request)
        {
            try
            {
                var result = await _gitService.CloneRepositoryAsync(request.GithubUrl, request.OutputDirectory);
                
                if (result.Success)
                {
                    // Get the directory tree structure for display
                    var treeStructure = GetDirectoryTreeStructure(result.ClonedDirectory);
                    
                    return Json(new { 
                        success = true, 
                        message = "Repository cloned successfully!",
                        clonedDirectory = result.ClonedDirectory,
                        repositoryName = result.RepositoryName,
                        treeStructure = treeStructure
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error during clone: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRepositoryTree([FromBody] GetRepositoryTreeRequest request)
        {
            try
            {
                if (!Directory.Exists(request.ClonedDirectory))
                {
                    return Json(new { success = false, message = "Repository directory not found." });
                }

                var treeStructure = GetDirectoryTreeStructure(request.ClonedDirectory);
                
                return Json(new { 
                    success = true, 
                    treeStructure = treeStructure
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error getting repository tree: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRepositoryItem([FromBody] RemoveRepositoryItemRequest request)
        {
            try
            {
                var fullPath = Path.Combine(request.ClonedDirectory, request.RelativePath);
                
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
                else if (System.IO.Directory.Exists(fullPath))
                {
                    System.IO.Directory.Delete(fullPath, true);
                }
                else
                {
                    return Json(new { success = false, message = "File or directory not found." });
                }

                var treeStructure = GetDirectoryTreeStructure(request.ClonedDirectory);
                
                return Json(new { 
                    success = true, 
                    message = "Item removed successfully.",
                    treeStructure = treeStructure
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error removing item: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ScoreRepositoryActivity([FromBody] ScoreRepositoryRequest request)
        {
            try
            {
                if (!Directory.Exists(request.ClonedDirectory))
                {
                    return Json(new { success = false, message = "Repository directory not found." });
                }

                // Find project directories (containing .csproj files)
                var projectDirectories = FindProjectDirectories(request.ClonedDirectory);
                var scannedDirectories = new List<string>();
                var scanLog = new List<string>();
                
                var rubric = JsonSerializer.Deserialize<List<RubricItem>>(request.RubricJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var scoringResults = new List<ScoringResult>();
                var totalScore = 0;
                var fileContents = new List<FileContent>();
                
                scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Starting repository scan...");
                scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Found {projectDirectories.Count} project director{(projectDirectories.Count == 1 ? "y" : "ies")}");

                // Scan each project directory
                foreach (var projectDir in projectDirectories)
                {
                    var relativeDir = Path.GetRelativePath(request.ClonedDirectory, projectDir);
                    scannedDirectories.Add(relativeDir);
                    
                    scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Scanning project directory: {relativeDir}");
                    
                    // Get all files from the project directory
                    var projectFiles = Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories)
                        .Where(f => !Path.GetFileName(f).StartsWith(".")) // Skip hidden files
                        .Where(f => !f.Contains("bin", StringComparison.OrdinalIgnoreCase)) // Skip bin folders
                        .Where(f => !f.Contains("obj", StringComparison.OrdinalIgnoreCase)) // Skip obj folders
                        .ToArray();
                        
                    scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Found {projectFiles.Length} files to analyze");

                    // Read all files from this project
                    foreach (var filePath in projectFiles)
                    {
                        try
                        {
                            var relativePath = Path.GetRelativePath(projectDir, filePath).Replace("\\", "/");
                            var content = await System.IO.File.ReadAllTextAsync(filePath);
                            fileContents.Add(new FileContent 
                            { 
                                Name = Path.GetFileName(filePath), 
                                Path = relativePath, 
                                Content = content,
                                ProjectDirectory = Path.GetRelativePath(request.ClonedDirectory, projectDir)
                            });
                            scanLog.Add($"[{DateTime.Now:HH:mm:ss}] ✓ Read file: {relativePath} ({content.Length} characters)");
                        }
                        catch (Exception ex)
                        {
                            // Skip binary files or files that can't be read as text
                            var relativePath = Path.GetRelativePath(projectDir, filePath).Replace("\\", "/");
                            scanLog.Add($"[{DateTime.Now:HH:mm:ss}] ✗ Skipped file: {relativePath} ({ex.Message})");
                            Console.WriteLine($"Skipping file {filePath}: {ex.Message}");
                        }
                    }
                }
                
                scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Total files loaded: {fileContents.Count}");
                scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Starting rubric evaluation...");

                // Score against the rubric
                scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Evaluating {rubric.Count} rubric criteria...");
                
                foreach (var item in rubric)
                {
                    scanLog.Add($"[{DateTime.Now:HH:mm:ss}] ── Criterion: '{item.Name}' ({item.Points} points)");
                    scanLog.Add($"[{DateTime.Now:HH:mm:ss}]    Keywords: [{string.Join(", ", item.Keywords)}]");
                    scanLog.Add($"[{DateTime.Now:HH:mm:ss}]    Target files: [{string.Join(", ", item.Files)}]");
                    
                    var criterionMet = false;
                    var proof = "N/A";
                    var fileName = "N/A";

                    foreach (var filePattern in item.Files)
                    {
                        var regex = new Regex(WildcardToRegex(filePattern));
                        var relevantFiles = fileContents.Where(f => !string.IsNullOrEmpty(f.Path) && regex.IsMatch(f.Path)).ToList();
                        
                        scanLog.Add($"[{DateTime.Now:HH:mm:ss}]    Searching pattern '{filePattern}': {relevantFiles.Count} matching files");

                        foreach (var file in relevantFiles)
                        {
                            scanLog.Add($"[{DateTime.Now:HH:mm:ss}]      Checking file: {file.Path}");
                            
                            var normalizedFileContent = Regex.Replace(file.Content, @"\s+", "").ToLower();
                            var foundKeywords = new List<string>();
                            var missingKeywords = new List<string>();
                            
                            foreach (var keyword in item.Keywords)
                            {
                                var normalizedKeyword = Regex.Replace(keyword, @"\s+", "").ToLower();
                                if (normalizedFileContent.Contains(normalizedKeyword))
                                {
                                    foundKeywords.Add(keyword);
                                }
                                else
                                {
                                    missingKeywords.Add(keyword);
                                }
                            }
                            
                            if (foundKeywords.Count > 0)
                            {
                                scanLog.Add($"[{DateTime.Now:HH:mm:ss}]        ✓ Found keywords: [{string.Join(", ", foundKeywords)}]");
                            }
                            if (missingKeywords.Count > 0)
                            {
                                scanLog.Add($"[{DateTime.Now:HH:mm:ss}]        ✗ Missing keywords: [{string.Join(", ", missingKeywords)}]");
                            }
                            
                            var allKeywordsFound = missingKeywords.Count == 0;

                            if (allKeywordsFound)
                            {
                                totalScore += item.Points;
                                proof = GetLineWithKeyword(file.Content, item.Keywords.First());
                                fileName = file.Name;
                                criterionMet = true;
                                scanLog.Add($"[{DateTime.Now:HH:mm:ss}]        ✓ CRITERION MET! Awarded {item.Points} points");
                                break;
                            }
                        }
                        if (criterionMet) break;
                    }
                    
                    if (!criterionMet)
                    {
                        scanLog.Add($"[{DateTime.Now:HH:mm:ss}]    ✗ Criterion not met (0 points)");
                    }
                    
                    scoringResults.Add(new ScoringResult { FileName = fileName, Criterion = item.Name, Points = criterionMet ? item.Points : 0, Proof = proof, Met = criterionMet });
                }
                
                scanLog.Add($"[{DateTime.Now:HH:mm:ss}] Scoring complete! Final score: {totalScore}/{rubric.Sum(r => r.Points)}");

                return Json(new { 
                    success = true, 
                    totalScore, 
                    results = scoringResults,
                    scannedDirectories = scannedDirectories,
                    projectCount = projectDirectories.Count,
                    scanLog = scanLog
                });
            }
            catch (JsonException)
            {
                return Json(new { success = false, message = "Invalid JSON format in rubric." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error during scoring: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetActivityTemplateRubric([FromBody] GetActivityTemplateRubricRequest request)
        {
            try
            {
                var activityTemplate = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Name == request.ActivityName && at.IsActive);

                if (activityTemplate != null && !string.IsNullOrEmpty(activityTemplate.RubricJson))
                {
                    return Json(new { 
                        success = true, 
                        rubricJson = activityTemplate.RubricJson 
                    });
                }
                else
                {
                    return Json(new { success = false, message = "No rubric found for this activity." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error getting rubric: {ex.Message}" });
            }
        }

        private List<string> FindProjectDirectories(string rootDirectory)
        {
            var projectDirectories = new List<string>();
            
            try
            {
                // Search for .csproj files recursively
                var csprojFiles = Directory.GetFiles(rootDirectory, "*.csproj", SearchOption.AllDirectories);
                
                foreach (var csprojFile in csprojFiles)
                {
                    var projectDir = Path.GetDirectoryName(csprojFile);
                    if (projectDir != null && !projectDirectories.Contains(projectDir))
                    {
                        projectDirectories.Add(projectDir);
                    }
                }
                
                // If no .csproj files found, fall back to the root directory
                if (projectDirectories.Count == 0)
                {
                    projectDirectories.Add(rootDirectory);
                }
            }
            catch (Exception)
            {
                // If there's an error, fall back to the root directory
                projectDirectories.Add(rootDirectory);
            }
            
            return projectDirectories;
        }

        private object GetDirectoryTreeStructure(string directoryPath)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(directoryPath);
                return GetDirectoryNode(directoryInfo, directoryPath);
            }
            catch (Exception)
            {
                return new { name = Path.GetFileName(directoryPath), type = "error", children = new object[0] };
            }
        }

        private object GetDirectoryNode(DirectoryInfo directory, string basePath)
        {
            try
            {
                var children = new List<object>();
                
                // Add subdirectories
                foreach (var subDir in directory.GetDirectories().Where(d => !d.Name.StartsWith(".")))
                {
                    children.Add(GetDirectoryNode(subDir, basePath));
                }
                
                // Add files
                foreach (var file in directory.GetFiles().Where(f => !f.Name.StartsWith(".")))
                {
                    var relativePath = Path.GetRelativePath(basePath, file.FullName).Replace("\\", "/");
                    children.Add(new 
                    { 
                        name = file.Name, 
                        type = "file", 
                        path = relativePath,
                        size = file.Length 
                    });
                }
                
                var relativeDir = Path.GetRelativePath(basePath, directory.FullName).Replace("\\", "/");
                if (relativeDir == ".")
                    relativeDir = "";
                
                return new 
                { 
                    name = directory.Name, 
                    type = "directory", 
                    path = relativeDir,
                    children = children.ToArray()
                };
            }
            catch (Exception)
            {
                return new { name = directory.Name, type = "error", children = new object[0] };
            }
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
        public string ProjectDirectory { get; set; }
    }

    public class ScoringResult
    {
        public string FileName { get; set; }
        public string Criterion { get; set; }
        public int Points { get; set; }
        [JsonPropertyName("proof")]
        public string Proof { get; set; }
        [JsonPropertyName("met")]
        public bool Met { get; set; }
    }

    public class CloneRepositoryRequest
    {
        public string GithubUrl { get; set; }
        public string OutputDirectory { get; set; }
    }

    public class GetRepositoryTreeRequest
    {
        public string ClonedDirectory { get; set; }
    }

    public class RemoveRepositoryItemRequest
    {
        public string ClonedDirectory { get; set; }
        public string RelativePath { get; set; }
    }

    public class ScoreRepositoryRequest
    {
        public string ClonedDirectory { get; set; }
        public string RubricJson { get; set; }
    }

    public class GetActivityTemplateRubricRequest
    {
        public string ActivityName { get; set; }
    }
}
