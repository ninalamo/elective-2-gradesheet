using elective_2_gradesheet.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Serialization;

namespace elective_2_gradesheet.Models
{
    public class BulkGradingViewModel
    {
        public int? SectionId { get; set; }
        public int? ActivityTemplateId { get; set; }
        public List<SelectListItem> Sections { get; set; } = [];
        public List<SelectListItem> ActivityTemplates { get; set; } = [];
        public List<BulkGradingStudentViewModel> Students { get; set; } = [];
        public string? ActivityTemplateName { get; set; }
        public string? StatusFilter { get; set; }
        public List<SelectListItem> StatusOptions { get; set; } = [];
    }

    public class BulkGradingStudentViewModel
    {
        [JsonPropertyName("studentId")]
        public int StudentId { get; set; }
        
        [JsonPropertyName("studentName")]
        public string StudentName { get; set; } = string.Empty;
        
        [JsonPropertyName("repositoryUrl")]
        public string? RepositoryUrl { get; set; }
        
        [JsonPropertyName("currentPoints")]
        public double? CurrentPoints { get; set; }
        
        [JsonPropertyName("currentStatus")]
        public string? CurrentStatus { get; set; }
        
        [JsonPropertyName("hasExistingSubmission")]
        public bool HasExistingSubmission { get; set; }
        
        [JsonPropertyName("submissionId")]
        public int? SubmissionId { get; set; }
        
        [JsonPropertyName("hasNonZeroGrade")]
        public bool HasNonZeroGrade { get; set; }
        
        [JsonPropertyName("hasTurnedIn")]
        public bool HasTurnedIn { get; set; }
        
        [JsonPropertyName("isSelected")]
        public bool IsSelected { get; set; }
        
        [JsonPropertyName("isVisible")]
        public bool IsVisible { get; set; }
    }

    public class BulkGradingStudentItem
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string? GitHubLink { get; set; }
        public double CurrentPoints { get; set; }
        public string CurrentStatus { get; set; } = "Missing";
        public bool HasExistingSubmission { get; set; }
        public bool IsSelected { get; set; }
        public bool HasNonZeroGrade => CurrentPoints > 0;
        public bool IsTurnedInWithGrade => CurrentStatus == "Turned In" && HasNonZeroGrade;
        public int? SubmissionId { get; set; }
    }

    public class BulkGradingRequest
    {
        public int SectionId { get; set; }
        public int ActivityTemplateId { get; set; }
        public List<BulkGradingSubmission> Submissions { get; set; } = [];
    }

    public class BulkGradingSubmission
    {
        public int StudentId { get; set; }
        public string? GitHubLink { get; set; }
    }

    public class BulkGradingResult
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public double Points { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> ScoringDetails { get; set; } = [];
        public string? RepositoryUrl { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsApproved { get; set; }
        public string? ClonedDirectory { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class BulkGradingProgressUpdate
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CurrentStep { get; set; } = string.Empty;
        public string? RepositoryUrl { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public double ProgressPercentage => TotalCount > 0 ? (ProcessedCount * 100.0) / TotalCount : 0;
        public bool IsComplete { get; set; }
        public bool HasError { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BulkGradingPreviewItem
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? RepositoryUrl { get; set; }
        public double NewPoints { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public double? CurrentPoints { get; set; }
        public string? CurrentStatus { get; set; }
        public bool IsNew { get; set; }
        public bool IsUpdate { get; set; }
        public List<string> ScoringDetails { get; set; } = [];
        public bool RequiresApproval { get; set; }
        public bool IsApproved { get; set; } = true;
    }

    public class BulkGradingSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public int ActivityTemplateId { get; set; }
        public int SectionId { get; set; }
        public List<BulkGradingPreviewItem> PreviewItems { get; set; } = [];
        public bool ShowNonZeroGrades { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public BulkGradingStatus Status { get; set; } = BulkGradingStatus.NotStarted;
    }

    public enum BulkGradingStatus
    {
        NotStarted,
        Processing,
        WaitingForApproval,
        Saving,
        Completed,
        Failed
    }
    
    // MVC Form Models
    public class BulkGradingFormModel
    {
        public int ActivityTemplateId { get; set; }
        public int SectionId { get; set; }
        public List<BulkGradingStudentFormModel> SelectedStudents { get; set; } = [];
    }
    
    public class BulkGradingStudentFormModel
    {
        public int StudentId { get; set; }
        public bool IsSelected { get; set; }
        public string? RepositoryUrl { get; set; }
    }
}
