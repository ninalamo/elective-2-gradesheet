using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace elective_2_gradesheet.Services
{
    /// <summary>
    /// Service interface for ActivityTemplate controller operations
    /// Combines CRUD operations with rubric validation and other business logic
    /// </summary>
    public interface IActivityTemplateControllerService
    {
        // CRUD Operations
        Task<(IEnumerable<ActivityTemplate> templates, IEnumerable<SelectListItem> sections)> GetActivityTemplatesForIndexAsync(
            string? searchString = null, int? sectionId = null, GradingPeriod? period = null, string? sortOrder = null);
        
        Task<ActivityTemplate?> GetActivityTemplateByIdAsync(int id);
        Task<ActivityTemplate?> GetActivityTemplateWithSectionAsync(int id);
        Task<IEnumerable<SelectListItem>> GetActiveSectionsAsync();
        Task<(bool success, string message)> CreateActivityTemplateAsync(ActivityTemplate template);
        Task<(bool success, string message)> UpdateActivityTemplateAsync(ActivityTemplate template);
        Task<(bool success, string message)> DeactivateActivityTemplateAsync(int id);
        Task<(bool success, string message, int newId)> DuplicateActivityTemplateAsync(int id);
        Task<bool> ActivityTemplateExistsAsync(int id);

        // Rubric Operations (from existing IActivityTemplateService)
        Task<RubricValidationResult> ValidateRubricJsonAsync(string rubricJson);
        Task<List<RubricItemViewModel>> ParseRubricJsonAsync(string rubricJson);
        Task<string> ConvertRubricItemsToJsonAsync(List<RubricItemViewModel> rubricItems);
        Task<string> FormatJsonAsync(string jsonString);
        Task<string> GetSampleRubricJsonAsync();
        Task<(bool IsValid, string ErrorMessage)> ValidateFilePatterns(List<string> filePatterns);
        Task<ActivityTemplateStatsViewModel> GetActivityTemplateStatsAsync();
        Task<List<string>> GetSuggestedFilePatternsAsync();
        Task<List<string>> GetSuggestedKeywordsAsync();
    }
}
