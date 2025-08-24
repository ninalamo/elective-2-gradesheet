using elective_2_gradesheet.Models;

namespace elective_2_gradesheet.Services
{
    public interface IActivityTemplateService
    {
        /// <summary>
        /// Validates the JSON structure of a rubric
        /// </summary>
        /// <param name="rubricJson">The JSON string to validate</param>
        /// <returns>Validation result with details</returns>
        Task<RubricValidationResult> ValidateRubricJsonAsync(string rubricJson);

        /// <summary>
        /// Parses rubric JSON and returns structured rubric items
        /// </summary>
        /// <param name="rubricJson">The JSON string to parse</param>
        /// <returns>List of rubric items</returns>
        Task<List<RubricItemViewModel>> ParseRubricJsonAsync(string rubricJson);

        /// <summary>
        /// Converts rubric items back to JSON format
        /// </summary>
        /// <param name="rubricItems">List of rubric items</param>
        /// <returns>JSON string representation</returns>
        Task<string> ConvertRubricItemsToJsonAsync(List<RubricItemViewModel> rubricItems);

        /// <summary>
        /// Formats and validates JSON string for better display
        /// </summary>
        /// <param name="jsonString">Raw JSON string</param>
        /// <returns>Formatted JSON string</returns>
        Task<string> FormatJsonAsync(string jsonString);

        /// <summary>
        /// Creates a sample rubric JSON for new templates
        /// </summary>
        /// <returns>Sample rubric JSON string</returns>
        Task<string> GetSampleRubricJsonAsync();

        /// <summary>
        /// Validates that file patterns are valid regex patterns
        /// </summary>
        /// <param name="filePatterns">List of file patterns to validate</param>
        /// <returns>Validation result</returns>
        Task<(bool IsValid, string ErrorMessage)> ValidateFilePatterns(List<string> filePatterns);

        /// <summary>
        /// Gets statistics about rubric usage across templates
        /// </summary>
        /// <returns>Activity template statistics</returns>
        Task<ActivityTemplateStatsViewModel> GetActivityTemplateStatsAsync();

        /// <summary>
        /// Suggests file patterns based on common programming file extensions
        /// </summary>
        /// <returns>List of suggested file patterns</returns>
        Task<List<string>> GetSuggestedFilePatternsAsync();

        /// <summary>
        /// Suggests common keywords for rubric items
        /// </summary>
        /// <returns>List of suggested keywords</returns>
        Task<List<string>> GetSuggestedKeywordsAsync();
    }
}
