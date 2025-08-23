using System.Diagnostics;
using elective_2_gradesheet.Models;
using Microsoft.Extensions.FileSystemGlobbing;

namespace elective_2_gradesheet.Services
{
    public interface IGitHubRepositoryService
    {
        Task<GitHubRepositoryResult> CloneAndScanRepositoryAsync(string githubUrl, string destinationPath, List<string> keywords = null);
        Task<SimpleRubricResult> CloneAndScanWithRubricAsync(string githubUrl, string destinationPath, List<SimpleRubricItem> rubric, int studentId, int activityId);
        Task<bool> IsValidGitHubUrlAsync(string githubUrl);
        Task<List<string>> ScanRepositoryForKeywordsAsync(string repositoryPath, List<string> keywords);
        Task CleanupRepositoryAsync(string repositoryPath);
    }

    public class GitHubRepositoryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ClonedPath { get; set; }
        public List<string> FoundKeywords { get; set; } = new List<string>();
        public List<GitHubFileResult> ScannedFiles { get; set; } = new List<GitHubFileResult>();
        public string ErrorDetails { get; set; }
    }

    public class GitHubFileResult
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public List<string> MatchedKeywords { get; set; } = new List<string>();
        public List<string> MatchedLines { get; set; } = new List<string>();
    }

    public class GitHubRepositoryService : IGitHubRepositoryService
    {
        private readonly ILogger<GitHubRepositoryService> _logger;

        public GitHubRepositoryService(ILogger<GitHubRepositoryService> logger)
        {
            _logger = logger;
        }

        public async Task<GitHubRepositoryResult> CloneAndScanRepositoryAsync(string githubUrl, string destinationPath, List<string> keywords = null)
        {
            var result = new GitHubRepositoryResult();

            try
            {
                // Validate GitHub URL
                if (!await IsValidGitHubUrlAsync(githubUrl))
                {
                    result.Success = false;
                    result.Message = "Invalid GitHub URL format.";
                    return result;
                }

                // Extract repository name from URL
                var repoName = ExtractRepositoryName(githubUrl);
                if (string.IsNullOrEmpty(repoName))
                {
                    result.Success = false;
                    result.Message = "Could not extract repository name from URL.";
                    return result;
                }

                // Ensure destination directory exists
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Create unique folder for this repository
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var repoPath = Path.Combine(destinationPath, $"{repoName}_{timestamp}");

                // Clone the repository
                var cloneSuccess = await CloneRepositoryAsync(githubUrl, repoPath);
                if (!cloneSuccess)
                {
                    result.Success = false;
                    result.Message = "Failed to clone repository. Please check the URL and your git installation.";
                    return result;
                }

                result.ClonedPath = repoPath;

                // Scan for keywords if provided
                if (keywords != null && keywords.Any())
                {
                    result.FoundKeywords = await ScanRepositoryForKeywordsAsync(repoPath, keywords);
                    result.ScannedFiles = await GetDetailedScanResults(repoPath, keywords);
                }

                result.Success = true;
                result.Message = $"Repository successfully cloned to {repoPath}";

                _logger.LogInformation($"Successfully cloned and scanned repository: {githubUrl} to {repoPath}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cloning repository {githubUrl}");
                result.Success = false;
                result.Message = "An error occurred while processing the repository.";
                result.ErrorDetails = ex.Message;
                return result;
            }
        }

        public async Task<bool> IsValidGitHubUrlAsync(string githubUrl)
        {
            if (string.IsNullOrWhiteSpace(githubUrl))
                return false;

            // Check if it's a valid GitHub URL format
            var patterns = new[]
            {
                @"^https://github\.com/[\w\-\.]+/[\w\-\.]+/?$",
                @"^https://github\.com/[\w\-\.]+/[\w\-\.]+\.git$",
                @"^git@github\.com:[\w\-\.]+/[\w\-\.]+\.git$"
            };

            return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(githubUrl, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        }

        public async Task<List<string>> ScanRepositoryForKeywordsAsync(string repositoryPath, List<string> keywords)
        {
            var foundKeywords = new List<string>();

            if (!Directory.Exists(repositoryPath) || keywords == null || !keywords.Any())
                return foundKeywords;

            try
            {
                // Get all text files in the repository (excluding .git folder)
                var textFiles = Directory.GetFiles(repositoryPath, "*", SearchOption.AllDirectories)
                    .Where(file => !file.Contains("\\.git\\") && IsTextFile(file))
                    .ToList();

                foreach (var file in textFiles)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var contentLower = content.ToLower();

                        foreach (var keyword in keywords)
                        {
                            if (contentLower.Contains(keyword.ToLower()) && !foundKeywords.Contains(keyword))
                            {
                                foundKeywords.Add(keyword);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Could not read file {file}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scanning repository {repositoryPath} for keywords");
            }

            return foundKeywords;
        }

        public async Task<SimpleRubricResult> CloneAndScanWithRubricAsync(string githubUrl, string destinationPath, List<SimpleRubricItem> rubric, int studentId, int activityId)
        {
            // Trim all strings in rubric before processing
            rubric = SimpleRubricHelper.TrimRubricItems(rubric);

            var result = new SimpleRubricResult
            {
                StudentId = studentId,
                ActivityId = activityId,
                RepositoryUrl = githubUrl,
                ScannedDate = DateTime.UtcNow
            };

            try
            {
                // First clone the repository - use empty keywords list for basic clone
                var cloneResult = await CloneAndScanRepositoryAsync(githubUrl, destinationPath, new List<string>());
                if (!cloneResult.Success)
                {
                    _logger.LogError($"Failed to clone repository for rubric scanning: {cloneResult.Message}");
                    return result; // Return empty result
                }

                var repoPath = cloneResult.ClonedPath;

                // Get all files in the repository (excluding .git)
                var allFiles = Directory.GetFiles(repoPath, "*", SearchOption.AllDirectories)
                    .Where(file => !file.Contains("\\.git\\"))
                    .Select(f => f.Replace("\\", "/")) // Normalize path separators
                    .ToList();

                double totalScore = 0;
                double maxPossibleScore = 0;

                // Process each rubric item
                foreach (var rubricItem in rubric)
                {
                    var itemResult = new RubricItemResult
                    {
                        Title = rubricItem.Title,
                        MaxScore = rubricItem.Score
                    };

                    // Find matching files for this rubric item
                    var matchingFiles = new List<string>();
                    var matcher = new Matcher();
                    
                    foreach (var filePattern in rubricItem.Files)
                    {
                        var pattern = filePattern.Trim();
                        if (pattern.Contains("*")) // Wildcard pattern
                        {
                            matcher.AddInclude(pattern);
                            var wildcardMatches = allFiles.Where(f =>
                            {
                                var relativePath = Path.GetRelativePath(repoPath, f).Replace("\\", "/");
                                return matcher.Match(relativePath).HasMatches;
                            }).ToList();
                            matchingFiles.AddRange(wildcardMatches);
                        }
                        else // Exact file match
                        {
                            var exactMatches = allFiles.Where(f => 
                                Path.GetFileName(f).Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
                            matchingFiles.AddRange(exactMatches);
                        }
                    }

                    matchingFiles = matchingFiles.Distinct().ToList();
                    itemResult.FoundFiles = matchingFiles.Select(f => Path.GetRelativePath(repoPath, f)).ToList();

                    // Track missing files
                    foreach (var filePattern in rubricItem.Files)
                    {
                        if (!filePattern.Contains("*")) // Only track specific files as missing
                        {
                            var found = matchingFiles.Any(f => 
                                Path.GetFileName(f).Equals(filePattern.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(filePattern.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (!found)
                            {
                                itemResult.MissingFiles.Add(filePattern.Trim());
                            }
                        }
                    }

                    // Scan for keywords in matching files
                    var foundKeywords = new HashSet<string>();
                    foreach (var file in matchingFiles)
                    {
                        try
                        {
                            var fileContent = await File.ReadAllTextAsync(file);
                            var lines = fileContent.Split('\n');
                            
                            for (int i = 0; i < lines.Length; i++)
                            {
                                var line = lines[i].Trim(); // Trim scanned file content
                                foreach (var keyword in rubricItem.Keywords)
                                {
                                    var trimmedKeyword = keyword.Trim(); // Trim keywords
                                    if (line.Contains(trimmedKeyword, StringComparison.OrdinalIgnoreCase))
                                    {
                                        foundKeywords.Add(trimmedKeyword);
                                        itemResult.KeywordMatches.Add(new KeywordMatch
                                        {
                                            Keyword = trimmedKeyword,
                                            LineNumber = i + 1,
                                            Line = line,
                                            Context = GetLineContext(lines, i)
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Could not read file {file}: {ex.Message}");
                        }
                    }

                    itemResult.FoundKeywords = foundKeywords.ToList();
                    itemResult.MissingKeywords = rubricItem.Keywords.Select(k => k.Trim())
                        .Except(foundKeywords).ToList();

                    // Calculate score for this item
                    var keywordScore = foundKeywords.Count > 0 ? rubricItem.Score : 0;
                    var fileScore = matchingFiles.Any() ? rubricItem.Score : 0;
                    
                    // Give partial credit: full score if both files and keywords found,
                    // half score if only files or only keywords found
                    if (matchingFiles.Any() && foundKeywords.Any())
                    {
                        itemResult.EarnedScore = rubricItem.Score; // Full score
                    }
                    else if (matchingFiles.Any() || foundKeywords.Any())
                    {
                        itemResult.EarnedScore = rubricItem.Score * 0.5; // Half score
                    }
                    else
                    {
                        itemResult.EarnedScore = 0; // No score
                    }

                    result.RubricItems.Add(itemResult);
                    totalScore += itemResult.EarnedScore;
                    maxPossibleScore += itemResult.MaxScore;
                }

                result.TotalScore = Math.Max(0, totalScore);
                result.MaxPossibleScore = maxPossibleScore;

                _logger.LogInformation($"Completed simple rubric scanning. Score: {result.TotalScore}/{result.MaxPossibleScore} ({result.Percentage:F1}%)");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scanning repository with simple rubric: {githubUrl}");
                return result; // Return what we have so far
            }
        }

        public async Task CleanupRepositoryAsync(string repositoryPath)
        {
            try
            {
                if (Directory.Exists(repositoryPath))
                {
                    // Remove read-only attributes from .git files
                    await Task.Run(() => RemoveReadOnlyAttributes(repositoryPath));
                    Directory.Delete(repositoryPath, true);
                    _logger.LogInformation($"Cleaned up repository at {repositoryPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cleaning up repository {repositoryPath}");
            }
        }

        private async Task<bool> CloneRepositoryAsync(string githubUrl, string destinationPath)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"clone \"{githubUrl}\" \"{destinationPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null) return false;

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation($"Git clone output: {output}");
                    return true;
                }
                else
                {
                    _logger.LogError($"Git clone failed. Exit code: {process.ExitCode}, Error: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing git clone command");
                return false;
            }
        }

        private string ExtractRepositoryName(string githubUrl)
        {
            try
            {
                var uri = new Uri(githubUrl.Replace(".git", ""));
                var segments = uri.Segments;
                if (segments.Length >= 2)
                {
                    return segments[^1].TrimEnd('/');
                }
            }
            catch
            {
                // Fallback for SSH URLs or malformed URLs
                var match = System.Text.RegularExpressions.Regex.Match(githubUrl, @"[:/]([\w\-\.]+)\.git$");
                if (match.Success)
                    return match.Groups[1].Value;
            }

            return null;
        }

        private bool IsTextFile(string filePath)
        {
            var textExtensions = new[] { ".txt", ".md", ".cs", ".js", ".ts", ".html", ".css", ".json", ".xml", ".yml", ".yaml", ".py", ".java", ".cpp", ".c", ".h", ".php", ".rb", ".go", ".rs", ".sh", ".bat", ".ps1", ".sql", ".config", ".gitignore", ".dockerfile", ".makefile" };
            var extension = Path.GetExtension(filePath).ToLower();
            
            return textExtensions.Contains(extension) || string.IsNullOrEmpty(extension);
        }

        private async Task<List<GitHubFileResult>> GetDetailedScanResults(string repositoryPath, List<string> keywords)
        {
            var results = new List<GitHubFileResult>();

            if (!Directory.Exists(repositoryPath) || keywords == null || !keywords.Any())
                return results;

            try
            {
                var textFiles = Directory.GetFiles(repositoryPath, "*", SearchOption.AllDirectories)
                    .Where(file => !file.Contains("\\.git\\") && IsTextFile(file))
                    .ToList();

                foreach (var file in textFiles)
                {
                    try
                    {
                        var lines = await File.ReadAllLinesAsync(file);
                        var fileResult = new GitHubFileResult
                        {
                            FilePath = file,
                            FileName = Path.GetFileName(file)
                        };

                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i];
                            var lineLower = line.ToLower();

                            foreach (var keyword in keywords)
                            {
                                if (lineLower.Contains(keyword.ToLower()))
                                {
                                    if (!fileResult.MatchedKeywords.Contains(keyword))
                                        fileResult.MatchedKeywords.Add(keyword);

                                    fileResult.MatchedLines.Add($"Line {i + 1}: {line.Trim()}");
                                }
                            }
                        }

                        if (fileResult.MatchedKeywords.Any())
                        {
                            results.Add(fileResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Could not scan file {file}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting detailed scan results for {repositoryPath}");
            }

            return results;
        }



        private string GetLineContext(string[] lines, int lineIndex, int contextLines = 1)
        {
            var start = Math.Max(0, lineIndex - contextLines);
            var end = Math.Min(lines.Length - 1, lineIndex + contextLines);
            
            var contextLines_list = new List<string>();
            for (int i = start; i <= end; i++)
            {
                var prefix = i == lineIndex ? ">>> " : "    ";
                contextLines_list.Add($"{prefix}{i + 1}: {lines[i].Trim()}");
            }
            
            return string.Join("\n", contextLines_list);
        }

        private void RemoveReadOnlyAttributes(string directoryPath)
        {
            foreach (var file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }
            }
        }
    }
}
