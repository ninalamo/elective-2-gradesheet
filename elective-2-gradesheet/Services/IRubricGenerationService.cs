using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using elective_2_gradesheet.Controllers;
using elective_2_gradesheet.Models;

namespace elective_2_gradesheet.Services
{
    public interface IRubricGenerationService
    {
        Task<(bool success, string message, List<RubricItem>? rubric)> GenerateRubricFromProjectAsync(IFormFile projectFile);
        Task<(bool success, string message, List<RubricItem>? rubric)> GenerateRubricFromDirectoryAsync(string projectPath);
    }

    public class RubricGenerationService : IRubricGenerationService
    {
        private readonly ILogger<RubricGenerationService> _logger;
        private static readonly string[] SupportedExtensions = { ".cs", ".js", ".ts", ".java", ".py", ".html", ".cshtml", ".json", ".xml", ".md" };

        public RubricGenerationService(ILogger<RubricGenerationService> logger)
        {
            _logger = logger;
        }

        public async Task<(bool success, string message, List<RubricItem>? rubric)> GenerateRubricFromProjectAsync(IFormFile projectFile)
        {
            if (projectFile == null || projectFile.Length == 0)
            {
                return (false, "No project file provided", null);
            }

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(tempDir);

                // Extract uploaded file
                if (projectFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using var stream = projectFile.OpenReadStream();
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                    archive.ExtractToDirectory(tempDir);
                }
                else
                {
                    // Single file upload
                    var filePath = Path.Combine(tempDir, projectFile.FileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await projectFile.CopyToAsync(stream);
                }

                return await GenerateRubricFromDirectoryAsync(tempDir);
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        public async Task<(bool success, string message, List<RubricItem>? rubric)> GenerateRubricFromDirectoryAsync(string projectPath)
        {
            try
            {
                var analysisResult = await AnalyzeProjectAsync(projectPath);
                var rubric = GenerateRubricFromAnalysis(analysisResult);
                
                return (true, $"Generated rubric with {rubric.Count} criteria based on project analysis", rubric);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating rubric from project at {ProjectPath}", projectPath);
                return (false, $"Error analyzing project: {ex.Message}", null);
            }
        }

        private async Task<ProjectAnalysis> AnalyzeProjectAsync(string projectPath)
        {
            var analysis = new ProjectAnalysis();
            var files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories)
                .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLower()))
                .Where(f => !IsInExcludedDirectory(f))
                .ToArray();

            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var extension = Path.GetExtension(file).ToLower();
                    var fileName = Path.GetFileName(file);

                    analysis.TotalFiles++;
                    analysis.FilesByType[extension] = analysis.FilesByType.GetValueOrDefault(extension, 0) + 1;

                    // Analyze content based on file type
                    switch (extension)
                    {
                        case ".cs":
                            AnalyzeCSharpFile(content, fileName, analysis);
                            break;
                        case ".js":
                        case ".ts":
                            AnalyzeJavaScriptFile(content, fileName, analysis);
                            break;
                        case ".java":
                            AnalyzeJavaFile(content, fileName, analysis);
                            break;
                        case ".py":
                            AnalyzePythonFile(content, fileName, analysis);
                            break;
                        case ".html":
                        case ".cshtml":
                            AnalyzeHtmlFile(content, fileName, analysis);
                            break;
                        case ".json":
                            AnalyzeJsonFile(content, fileName, analysis);
                            break;
                        case ".md":
                            AnalyzeDocumentationFile(content, fileName, analysis);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error analyzing file {File}", file);
                }
            }

            return analysis;
        }

        private void AnalyzeCSharpFile(string content, string fileName, ProjectAnalysis analysis)
        {
            // Classes
            var classMatches = Regex.Matches(content, @"public\s+class\s+\w+", RegexOptions.IgnoreCase);
            analysis.ClassCount += classMatches.Count;
            if (classMatches.Count > 0) analysis.Features.Add("Class Definition");

            // Methods
            var methodMatches = Regex.Matches(content, @"(public|private|protected)\s+[\w<>\[\]]+\s+\w+\s*\([^)]*\)", RegexOptions.IgnoreCase);
            analysis.MethodCount += methodMatches.Count;
            if (methodMatches.Count > 0) analysis.Features.Add("Method Implementation");

            // Properties
            var propertyMatches = Regex.Matches(content, @"(public|private|protected)\s+[\w<>\[\]]+\s+\w+\s*{\s*(get|set)", RegexOptions.IgnoreCase);
            analysis.PropertyCount += propertyMatches.Count;
            if (propertyMatches.Count > 0) analysis.Features.Add("Property Definition");

            // Constructors
            if (content.Contains("public " + Path.GetFileNameWithoutExtension(fileName) + "("))
            {
                analysis.Features.Add("Constructor Implementation");
            }

            // Error handling
            if (content.Contains("try") && content.Contains("catch"))
            {
                analysis.Features.Add("Error Handling");
            }

            // LINQ usage
            if (Regex.IsMatch(content, @"\.\w*(Where|Select|OrderBy|GroupBy|Join)", RegexOptions.IgnoreCase))
            {
                analysis.Features.Add("LINQ Usage");
            }

            // Database/Entity Framework
            if (content.Contains("DbContext") || content.Contains("DbSet") || content.Contains("[Key]"))
            {
                analysis.Features.Add("Database Integration");
            }

            // MVC Controller
            if (content.Contains("Controller") && content.Contains("IActionResult"))
            {
                analysis.Features.Add("MVC Controller");
            }

            // Model classes
            if (content.Contains("[Required]") || content.Contains("DataAnnotations"))
            {
                analysis.Features.Add("Data Validation");
            }

            // Dependency Injection
            if (content.Contains("private readonly") && content.Contains("constructor"))
            {
                analysis.Features.Add("Dependency Injection");
            }
        }

        private void AnalyzeJavaScriptFile(string content, string fileName, ProjectAnalysis analysis)
        {
            // Function definitions
            var functionMatches = Regex.Matches(content, @"function\s+\w+\s*\(", RegexOptions.IgnoreCase);
            analysis.MethodCount += functionMatches.Count;
            if (functionMatches.Count > 0) analysis.Features.Add("JavaScript Functions");

            // Arrow functions
            if (content.Contains("=>"))
            {
                analysis.Features.Add("Arrow Functions");
            }

            // DOM manipulation
            if (content.Contains("document.") || content.Contains("$"))
            {
                analysis.Features.Add("DOM Manipulation");
            }

            // AJAX/API calls
            if (content.Contains("fetch") || content.Contains("$.ajax") || content.Contains("XMLHttpRequest"))
            {
                analysis.Features.Add("API Integration");
            }
        }

        private void AnalyzeJavaFile(string content, string fileName, ProjectAnalysis analysis)
        {
            var classMatches = Regex.Matches(content, @"public\s+class\s+\w+", RegexOptions.IgnoreCase);
            analysis.ClassCount += classMatches.Count;
            if (classMatches.Count > 0) analysis.Features.Add("Java Class Definition");

            var methodMatches = Regex.Matches(content, @"(public|private|protected)\s+[\w<>\[\]]+\s+\w+\s*\([^)]*\)", RegexOptions.IgnoreCase);
            analysis.MethodCount += methodMatches.Count;
            if (methodMatches.Count > 0) analysis.Features.Add("Java Method Implementation");
        }

        private void AnalyzePythonFile(string content, string fileName, ProjectAnalysis analysis)
        {
            var classMatches = Regex.Matches(content, @"class\s+\w+", RegexOptions.IgnoreCase);
            analysis.ClassCount += classMatches.Count;
            if (classMatches.Count > 0) analysis.Features.Add("Python Class Definition");

            var functionMatches = Regex.Matches(content, @"def\s+\w+\s*\(", RegexOptions.IgnoreCase);
            analysis.MethodCount += functionMatches.Count;
            if (functionMatches.Count > 0) analysis.Features.Add("Python Function Implementation");
        }

        private void AnalyzeHtmlFile(string content, string fileName, ProjectAnalysis analysis)
        {
            if (content.Contains("@model") || content.Contains("@{"))
            {
                analysis.Features.Add("Razor Views");
            }

            if (content.Contains("bootstrap") || content.Contains("btn") || content.Contains("container"))
            {
                analysis.Features.Add("Bootstrap Integration");
            }

            if (content.Contains("<form") && content.Contains("method="))
            {
                analysis.Features.Add("Form Implementation");
            }

            if (content.Contains("<table"))
            {
                analysis.Features.Add("Table Structure");
            }
        }

        private void AnalyzeJsonFile(string content, string fileName, ProjectAnalysis analysis)
        {
            if (fileName.Contains("package"))
            {
                analysis.Features.Add("Package Configuration");
            }
            else if (fileName.Contains("appsettings"))
            {
                analysis.Features.Add("Application Configuration");
            }
            else
            {
                analysis.Features.Add("JSON Configuration");
            }
        }

        private void AnalyzeDocumentationFile(string content, string fileName, ProjectAnalysis analysis)
        {
            if (content.Length > 100) // Has substantial content
            {
                analysis.Features.Add("Documentation");
            }
        }

        private List<RubricItem> GenerateRubricFromAnalysis(ProjectAnalysis analysis)
        {
            var rubric = new List<RubricItem>();
            var totalPoints = 100;
            var usedPoints = 0;

            // Generate rubric items based on detected features
            foreach (var feature in analysis.Features)
            {
                var rubricItem = CreateRubricItem(feature, analysis);
                if (rubricItem != null)
                {
                    rubric.Add(rubricItem);
                    usedPoints += rubricItem.Points;
                }
            }

            // Adjust points to total 100
            if (usedPoints > 0)
            {
                AdjustPointsToTotal(rubric, totalPoints);
            }
            else
            {
                // Fallback basic rubric
                rubric = CreateBasicRubric(analysis);
            }

            return rubric.OrderByDescending(r => r.Points).ToList();
        }

        private RubricItem? CreateRubricItem(string feature, ProjectAnalysis analysis)
        {
            return feature switch
            {
                "Class Definition" or "Java Class Definition" or "Python Class Definition" => new RubricItem
                {
                    Name = "Class Definition",
                    Points = 20,
                    Keywords = ["public class", "class"],
                    Files = ["*.cs", "*.java", "*.py"]
                },
                "Method Implementation" or "Java Method Implementation" or "Python Function Implementation" or "JavaScript Functions" => new RubricItem
                {
                    Name = "Method Implementation",
                    Points = 20,
                    Keywords = ["public", "method", "function", "def"],
                    Files = ["*.cs", "*.java", "*.py", "*.js", "*.ts"]
                },
                "Property Definition" => new RubricItem
                {
                    Name = "Property Definition",
                    Points = 15,
                    Keywords = ["get", "set", "public"],
                    Files = ["*.cs", "*.java"]
                },
                "Constructor Implementation" => new RubricItem
                {
                    Name = "Constructor Implementation",
                    Points = 15,
                    Keywords = ["public", "constructor", "("],
                    Files = ["*.cs", "*.java"]
                },
                "Error Handling" => new RubricItem
                {
                    Name = "Error Handling",
                    Points = 15,
                    Keywords = ["try", "catch", "exception", "error"],
                    Files = ["*.cs", "*.java", "*.py", "*.js", "*.ts"]
                },
                "MVC Controller" => new RubricItem
                {
                    Name = "MVC Controller Implementation",
                    Points = 25,
                    Keywords = ["Controller", "IActionResult", "HttpGet", "HttpPost"],
                    Files = ["*Controller.cs"]
                },
                "Razor Views" => new RubricItem
                {
                    Name = "Razor View Implementation",
                    Points = 20,
                    Keywords = ["@model", "@{", "@foreach", "ViewData"],
                    Files = ["*.cshtml"]
                },
                "Database Integration" => new RubricItem
                {
                    Name = "Database Integration",
                    Points = 20,
                    Keywords = ["DbContext", "DbSet", "[Key]", "Entity"],
                    Files = ["*.cs"]
                },
                "Data Validation" => new RubricItem
                {
                    Name = "Data Validation",
                    Points = 10,
                    Keywords = ["[Required]", "[MaxLength]", "ValidationAttribute", "ModelState"],
                    Files = ["*.cs"]
                },
                "LINQ Usage" => new RubricItem
                {
                    Name = "LINQ Implementation",
                    Points = 15,
                    Keywords = ["Where", "Select", "OrderBy", "GroupBy", "FirstOrDefault"],
                    Files = ["*.cs"]
                },
                "Form Implementation" => new RubricItem
                {
                    Name = "Form Implementation",
                    Points = 10,
                    Keywords = ["<form", "method=", "asp-action", "input"],
                    Files = ["*.html", "*.cshtml"]
                },
                "Bootstrap Integration" => new RubricItem
                {
                    Name = "Bootstrap Styling",
                    Points = 5,
                    Keywords = ["bootstrap", "btn", "container", "card", "table"],
                    Files = ["*.html", "*.cshtml"]
                },
                "API Integration" => new RubricItem
                {
                    Name = "API Integration",
                    Points = 15,
                    Keywords = ["fetch", "ajax", "API", "JSON", "HttpClient"],
                    Files = ["*.js", "*.ts", "*.cs"]
                },
                "Documentation" => new RubricItem
                {
                    Name = "Documentation",
                    Points = 10,
                    Keywords = ["README", "documentation", "//", "///", "/*"],
                    Files = ["*.md", "*.txt", "*.cs", "*.js"]
                },
                _ => null
            };
        }

        private List<RubricItem> CreateBasicRubric(ProjectAnalysis analysis)
        {
            return new List<RubricItem>
            {
                new RubricItem
                {
                    Name = "Code Structure",
                    Points = 30,
                    Keywords = ["class", "function", "method", "public"],
                    Files = ["*.cs", "*.js", "*.py", "*.java"]
                },
                new RubricItem
                {
                    Name = "Implementation Quality",
                    Points = 25,
                    Keywords = ["return", "if", "for", "while"],
                    Files = ["*.cs", "*.js", "*.py", "*.java"]
                },
                new RubricItem
                {
                    Name = "Error Handling",
                    Points = 20,
                    Keywords = ["try", "catch", "exception", "error"],
                    Files = ["*.cs", "*.js", "*.py", "*.java"]
                },
                new RubricItem
                {
                    Name = "Code Comments",
                    Points = 15,
                    Keywords = ["//", "/*", "///", "#"],
                    Files = ["*.cs", "*.js", "*.py", "*.java"]
                },
                new RubricItem
                {
                    Name = "Project Organization",
                    Points = 10,
                    Keywords = ["namespace", "using", "import", "require"],
                    Files = ["*.cs", "*.js", "*.py", "*.java"]
                }
            };
        }

        private void AdjustPointsToTotal(List<RubricItem> rubric, int targetTotal)
        {
            var currentTotal = rubric.Sum(r => r.Points);
            if (currentTotal == 0) return;

            var factor = (double)targetTotal / currentTotal;
            
            foreach (var item in rubric)
            {
                item.Points = Math.Max(5, (int)Math.Round(item.Points * factor));
            }

            // Final adjustment to ensure exact total
            var finalTotal = rubric.Sum(r => r.Points);
            var difference = targetTotal - finalTotal;
            
            if (difference != 0 && rubric.Any())
            {
                // Add/subtract from the largest item
                var largestItem = rubric.OrderByDescending(r => r.Points).First();
                largestItem.Points = Math.Max(5, largestItem.Points + difference);
            }
        }

        private bool IsInExcludedDirectory(string filePath)
        {
            var excludedDirs = new[] { "bin", "obj", "node_modules", ".git", ".vs", "packages", "__pycache__" };
            var pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return pathParts.Any(part => excludedDirs.Contains(part.ToLower()));
        }
    }

    public class ProjectAnalysis
    {
        public int TotalFiles { get; set; }
        public Dictionary<string, int> FilesByType { get; set; } = new();
        public HashSet<string> Features { get; set; } = new();
        public int ClassCount { get; set; }
        public int MethodCount { get; set; }
        public int PropertyCount { get; set; }
    }
}
