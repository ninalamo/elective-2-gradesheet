using System.Text.Json;
using System.Text.Json.Serialization;

namespace elective_2_gradesheet.Models
{
    /// <summary>
    /// Simple rubric structure: array of items with title, score, files to check, and keywords
    /// </summary>
    public class SimpleRubricItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; } // Points if perfect

        [JsonPropertyName("files")]
        public List<string> Files { get; set; } = new List<string>(); // Files to check

        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new List<string>(); // Keywords to find in files
    }

    /// <summary>
    /// Results from scanning a student's repository with simple rubric
    /// </summary>
    public class SimpleRubricResult
    {
        [JsonPropertyName("studentId")]
        public int StudentId { get; set; }

        [JsonPropertyName("activityId")]
        public int ActivityId { get; set; }

        [JsonPropertyName("repositoryUrl")]
        public string RepositoryUrl { get; set; } = string.Empty;

        [JsonPropertyName("scannedDate")]
        public DateTime ScannedDate { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("rubricItems")]
        public List<RubricItemResult> RubricItems { get; set; } = new List<RubricItemResult>();

        [JsonPropertyName("totalScore")]
        public double TotalScore { get; set; }

        [JsonPropertyName("maxPossibleScore")]
        public double MaxPossibleScore { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage => MaxPossibleScore > 0 ? (TotalScore / MaxPossibleScore) * 100 : 0;
    }

    /// <summary>
    /// Result for a single rubric item
    /// </summary>
    public class RubricItemResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("maxScore")]
        public double MaxScore { get; set; }

        [JsonPropertyName("earnedScore")]
        public double EarnedScore { get; set; }

        [JsonPropertyName("foundFiles")]
        public List<string> FoundFiles { get; set; } = new List<string>();

        [JsonPropertyName("missingFiles")]
        public List<string> MissingFiles { get; set; } = new List<string>();

        [JsonPropertyName("foundKeywords")]
        public List<string> FoundKeywords { get; set; } = new List<string>();

        [JsonPropertyName("missingKeywords")]
        public List<string> MissingKeywords { get; set; } = new List<string>();

        [JsonPropertyName("keywordMatches")]
        public List<KeywordMatch> KeywordMatches { get; set; } = new List<KeywordMatch>();
    }


    /// <summary>
    /// Represents a keyword match in a file
    /// </summary>
    public class KeywordMatch
    {
        [JsonPropertyName("keyword")]
        public string Keyword { get; set; } = string.Empty;

        [JsonPropertyName("lineNumber")]
        public int LineNumber { get; set; }

        [JsonPropertyName("line")]
        public string Line { get; set; } = string.Empty;

        [JsonPropertyName("context")]
        public string? Context { get; set; } // Additional context around the match
    }


    /// <summary>
    /// Utility class for working with simple rubrics and results
    /// </summary>
    public static class SimpleRubricHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        /// <summary>
        /// Deserialize JSON string to simple rubric array
        /// </summary>
        public static List<SimpleRubricItem>? DeserializeRubric(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<List<SimpleRubricItem>>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Serialize simple rubric to JSON string
        /// </summary>
        public static string SerializeRubric(List<SimpleRubricItem> rubric)
        {
            return JsonSerializer.Serialize(rubric, JsonOptions);
        }

        /// <summary>
        /// Serialize scanning results to JSON string
        /// </summary>
        public static string SerializeResult(SimpleRubricResult result)
        {
            return JsonSerializer.Serialize(result, JsonOptions);
        }

        /// <summary>
        /// Create a sample simple rubric for demonstration
        /// </summary>
        public static List<SimpleRubricItem> CreateSampleRubric()
        {
            return new List<SimpleRubricItem>
            {
                new SimpleRubricItem
                {
                    Title = "HTML Structure",
                    Score = 25,
                    Files = new List<string> { "index.html", "*.html" },
                    Keywords = new List<string> { "<!DOCTYPE html>", "<html>", "<head>", "<body>", "<meta charset" }
                },
                new SimpleRubricItem
                {
                    Title = "CSS Styling",
                    Score = 20,
                    Files = new List<string> { "style.css", "*.css" },
                    Keywords = new List<string> { "flexbox", "grid", "@media", "responsive" }
                },
                new SimpleRubricItem
                {
                    Title = "JavaScript Functionality",
                    Score = 30,
                    Files = new List<string> { "script.js", "*.js" },
                    Keywords = new List<string> { "function", "addEventListener", "querySelector", "async", "fetch" }
                },
                new SimpleRubricItem
                {
                    Title = "Documentation",
                    Score = 15,
                    Files = new List<string> { "README.md", "readme.txt" },
                    Keywords = new List<string> { "# ", "## ", "description", "usage", "installation" }
                }
            };
        }

        /// <summary>
        /// Trim whitespace from strings in files and keywords lists
        /// </summary>
        public static List<SimpleRubricItem> TrimRubricItems(List<SimpleRubricItem> rubric)
        {
            foreach (var item in rubric)
            {
                item.Title = item.Title?.Trim() ?? string.Empty;
                item.Files = item.Files.Select(f => f?.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToList()!;
                item.Keywords = item.Keywords.Select(k => k?.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList()!;
            }
            return rubric;
        }
    }
}
