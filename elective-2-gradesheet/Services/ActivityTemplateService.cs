using System.Text.Json;
using System.Text.RegularExpressions;
using elective_2_gradesheet.Data;
using elective_2_gradesheet.Models;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Services
{
    public class ActivityTemplateService : IActivityTemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ActivityTemplateService> _logger;

        public ActivityTemplateService(ApplicationDbContext context, ILogger<ActivityTemplateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RubricValidationResult> ValidateRubricJsonAsync(string rubricJson)
        {
            var result = new RubricValidationResult();

            try
            {
                if (string.IsNullOrWhiteSpace(rubricJson))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Rubric JSON cannot be empty.";
                    return result;
                }

                // Parse JSON to validate structure
                var jsonDocument = JsonDocument.Parse(rubricJson);
                
                if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Rubric JSON must be an array of rubric items.";
                    return result;
                }

                var rubricItems = new List<RubricItemViewModel>();
                int totalPoints = 0;

                foreach (var element in jsonDocument.RootElement.EnumerateArray())
                {
                    var rubricItem = ValidateRubricItem(element);
                    if (rubricItem == null)
                    {
                        result.IsValid = false;
                        result.ErrorMessage = "Invalid rubric item structure. Each item must have 'name', 'points', 'keywords' array, and 'files' array.";
                        return result;
                    }

                    rubricItems.Add(rubricItem);
                    totalPoints += rubricItem.Points;
                }

                // Validate total points equals 100
                if (totalPoints != 100)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Total rubric points must equal 100. Current total: {totalPoints}";
                    result.TotalPoints = totalPoints;
                    return result;
                }

                // Validate file patterns
                var allFilePatterns = rubricItems.SelectMany(ri => ri.Files).ToList();
                var filePatternValidation = await ValidateFilePatterns(allFilePatterns);
                if (!filePatternValidation.IsValid)
                {
                    result.IsValid = false;
                    result.ErrorMessage = filePatternValidation.ErrorMessage;
                    return result;
                }

                result.IsValid = true;
                result.TotalPoints = totalPoints;
                result.RubricItems = rubricItems;

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON format in rubric");
                result.IsValid = false;
                result.ErrorMessage = $"Invalid JSON format: {ex.Message}";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating rubric JSON");
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                return result;
            }
        }

        public async Task<List<RubricItemViewModel>> ParseRubricJsonAsync(string rubricJson)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rubricJson))
                {
                    return new List<RubricItemViewModel>();
                }

                var jsonDocument = JsonDocument.Parse(rubricJson);
                var rubricItems = new List<RubricItemViewModel>();

                foreach (var element in jsonDocument.RootElement.EnumerateArray())
                {
                    var rubricItem = ValidateRubricItem(element);
                    if (rubricItem != null)
                    {
                        rubricItems.Add(rubricItem);
                    }
                }

                return rubricItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing rubric JSON");
                return new List<RubricItemViewModel>();
            }
        }

        public async Task<string> ConvertRubricItemsToJsonAsync(List<RubricItemViewModel> rubricItems)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var jsonItems = rubricItems.Select(ri => new
                {
                    name = ri.Name,
                    points = ri.Points,
                    keywords = ri.Keywords,
                    files = ri.Files
                });

                return JsonSerializer.Serialize(jsonItems, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting rubric items to JSON");
                throw;
            }
        }

        public async Task<string> FormatJsonAsync(string jsonString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return "[]";
                }

                var jsonDocument = JsonDocument.Parse(jsonString);
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                return JsonSerializer.Serialize(jsonDocument, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting JSON");
                throw new InvalidOperationException($"Invalid JSON format: {ex.Message}", ex);
            }
        }

        public async Task<string> GetSampleRubricJsonAsync()
        {
            var sampleRubric = new[]
            {
                new
                {
                    name = "Class Definition",
                    points = 20,
                    keywords = new[] { "public class", "class" },
                    files = new[] { "*.cs", "*.java", "*.py" }
                },
                new
                {
                    name = "Method Implementation",
                    points = 25,
                    keywords = new[] { "public", "method", "function", "def" },
                    files = new[] { "*.cs", "*.java", "*.py", "*.js" }
                },
                new
                {
                    name = "Error Handling",
                    points = 20,
                    keywords = new[] { "try", "catch", "exception", "error" },
                    files = new[] { "*.cs", "*.java", "*.py", "*.js" }
                },
                new
                {
                    name = "Documentation",
                    points = 15,
                    keywords = new[] { "///", "/**", "#", "comment" },
                    files = new[] { "*.cs", "*.java", "*.py", "*.js", "*.md" }
                },
                new
                {
                    name = "Testing",
                    points = 20,
                    keywords = new[] { "test", "assert", "expect", "should" },
                    files = new[] { "*Test*.cs", "*test*.java", "test_*.py", "*.test.js" }
                }
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(sampleRubric, options);
        }

        public async Task<(bool IsValid, string ErrorMessage)> ValidateFilePatterns(List<string> filePatterns)
        {
            try
            {
                foreach (var pattern in filePatterns)
                {
                    if (string.IsNullOrWhiteSpace(pattern))
                    {
                        return (false, "File patterns cannot be empty.");
                    }

                    try
                    {
                        // Convert wildcard pattern to regex to test if it's valid
                        var regexPattern = WildcardToRegex(pattern);
                        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                    }
                    catch (ArgumentException ex)
                    {
                        return (false, $"Invalid file pattern '{pattern}': {ex.Message}");
                    }
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file patterns");
                return (false, $"Error validating file patterns: {ex.Message}");
            }
        }

        public async Task<ActivityTemplateStatsViewModel> GetActivityTemplateStatsAsync()
        {
            try
            {
                var stats = new ActivityTemplateStatsViewModel();

                var templates = await _context.ActivityTemplates
                    .Include(at => at.Section)
                    .ToListAsync();

                stats.TotalTemplates = templates.Count;
                stats.ActiveTemplates = templates.Count(t => t.IsActive);
                stats.TemplatesWithRubrics = templates.Count(t => !string.IsNullOrWhiteSpace(t.RubricJson));
                stats.TemplatesWithoutRubrics = stats.TotalTemplates - stats.TemplatesWithRubrics;

                // Group by period
                stats.TemplatesByPeriod = templates
                    .GroupBy(t => t.Period)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Group by section
                stats.TemplatesBySection = templates
                    .GroupBy(t => t.Section?.Name ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count());

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting activity template stats");
                return new ActivityTemplateStatsViewModel();
            }
        }

        public async Task<List<string>> GetSuggestedFilePatternsAsync()
        {
            return new List<string>
            {
                "*.cs",      // C# files
                "*.java",    // Java files
                "*.py",      // Python files
                "*.js",      // JavaScript files
                "*.ts",      // TypeScript files
                "*.cpp",     // C++ files
                "*.c",       // C files
                "*.h",       // Header files
                "*.html",    // HTML files
                "*.css",     // CSS files
                "*.sql",     // SQL files
                "*.md",      // Markdown files
                "*.txt",     // Text files
                "*.json",    // JSON files
                "*.xml",     // XML files
                "*.yml",     // YAML files
                "*.yaml",    // YAML files
                "*Test*.cs", // C# test files
                "test_*.py", // Python test files
                "*.test.js", // JavaScript test files
                "**/src/**", // Source directories
                "**/test/**" // Test directories
            };
        }

        public async Task<List<string>> GetSuggestedKeywordsAsync()
        {
            return new List<string>
            {
                // Object-oriented programming
                "public class",
                "private class",
                "interface",
                "abstract class",
                "inheritance",
                "polymorphism",
                
                // Methods and functions
                "public method",
                "private method",
                "static method",
                "function",
                "def",
                "return",
                
                // Control structures
                "if statement",
                "for loop",
                "while loop",
                "switch",
                "case",
                
                // Error handling
                "try",
                "catch",
                "finally",
                "throw",
                "exception",
                "error handling",
                
                // Data structures
                "array",
                "list",
                "dictionary",
                "hash",
                "queue",
                "stack",
                
                // Database
                "SELECT",
                "INSERT",
                "UPDATE",
                "DELETE",
                "JOIN",
                "WHERE",
                
                // Testing
                "test",
                "assert",
                "expect",
                "should",
                "unit test",
                "integration test",
                
                // Documentation
                "comment",
                "documentation",
                "///",
                "/**",
                "#"
            };
        }

        private RubricItemViewModel? ValidateRubricItem(JsonElement element)
        {
            try
            {
                if (!element.TryGetProperty("name", out var nameProperty) ||
                    !element.TryGetProperty("points", out var pointsProperty) ||
                    !element.TryGetProperty("keywords", out var keywordsProperty) ||
                    !element.TryGetProperty("files", out var filesProperty))
                {
                    return null;
                }

                var name = nameProperty.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                if (!pointsProperty.TryGetInt32(out var points) || points <= 0 || points > 100)
                {
                    return null;
                }

                if (keywordsProperty.ValueKind != JsonValueKind.Array || 
                    filesProperty.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                var keywords = keywordsProperty.EnumerateArray()
                    .Select(k => k.GetString())
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Select(k => k!)
                    .ToList();

                var files = filesProperty.EnumerateArray()
                    .Select(f => f.GetString())
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Select(f => f!)
                    .ToList();

                if (keywords.Count == 0 || files.Count == 0)
                {
                    return null;
                }

                return new RubricItemViewModel
                {
                    Name = name,
                    Points = points,
                    Keywords = keywords,
                    Files = files
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating rubric item");
                return null;
            }
        }

        private static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }
    }
}
