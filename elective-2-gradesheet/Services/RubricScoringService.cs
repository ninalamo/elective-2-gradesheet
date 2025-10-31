using elective_2_gradesheet.Models;
using System.Text.RegularExpressions;

namespace elective_2_gradesheet.Services;

public class RubricScoringService
{
    public async Task<RubricScoringResult> ScoreSubmissionAsync(List<RubricItem> rubric, List<FileContent> fileContents)
    {
        try
        {
            var scoringDetails = new List<string>();
            var totalPoints = 0.0;

            foreach (var item in rubric)
            {
                scoringDetails.Add($"── Criterion: '{item.Name}' ({item.Points} points)");
                scoringDetails.Add($"   Keywords: [{string.Join(", ", item.Keywords)}]");
                scoringDetails.Add($"   Target files: [{string.Join(", ", item.Files)}]");

                var criterionMet = false;
                var proof = "N/A";
                var fileName = "N/A";

                foreach (var filePattern in item.Files)
                {
                    var regex = new Regex(WildcardToRegex(filePattern));
                    var relevantFiles = fileContents.Where(f => !string.IsNullOrEmpty(f.Path) && 
                                                                regex.IsMatch(f.Path.Replace("\\", "/"))).ToList();

                    foreach (var file in relevantFiles)
                    {
                        var normalizedFileContent = Regex.Replace(file.Content, @"\s+", "").ToLower();
                        
                        // Count how many keywords are found
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

                        // Calculate score based on found keywords
                        if (foundKeywords.Count > 0)
                        {
                            var keywordScore = item.Points - missingKeywords.Count;
                            // Don't allow negative scores, minimum is 0
                            keywordScore = Math.Max(0, keywordScore);
                            
                            totalPoints += keywordScore;
                            proof = GetLineWithKeyword(file.Content, foundKeywords.First());
                            fileName = file.Name;
                            criterionMet = true;

                            scoringDetails.Add($"   ✓ Found in {fileName}: {foundKeywords.Count}/{item.Keywords.Count} keywords");
                            scoringDetails.Add($"   ✓ Score: {keywordScore} points");
                            if (missingKeywords.Any())
                            {
                                scoringDetails.Add($"   ⚠ Missing keywords: [{string.Join(", ", missingKeywords)}]");
                            }
                            scoringDetails.Add($"   📝 Proof: {proof}");
                            break;
                        }
                    }
                    if (criterionMet) break;
                }
                
                // Only add negative result if we haven't already found it
                if (!criterionMet)
                {
                    scoringDetails.Add($"   ✗ Not found - 0 points");
                }

                scoringDetails.Add(""); // Add empty line for readability
            }

            return new RubricScoringResult
            {
                Success = true,
                TotalPoints = totalPoints,
                ScoringDetails = scoringDetails
            };
        }
        catch (Exception ex)
        {
            return new RubricScoringResult
            {
                Success = false,
                ErrorMessage = $"Error during rubric scoring: {ex.Message}",
                TotalPoints = 0,
                ScoringDetails = new List<string> { $"Error: {ex.Message}" }
            };
        }
    }

    private string GetLineWithKeyword(string content, string keyword)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        return lines.FirstOrDefault(line => line.ToLower().Contains(keyword.ToLower()))?.Trim() ?? "";
    }

    private static string WildcardToRegex(string pattern)
    {
        return Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    }
}


