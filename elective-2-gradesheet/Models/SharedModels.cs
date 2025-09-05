using System.Text.Json.Serialization;

namespace elective_2_gradesheet.Models;

public class RubricItem
{
    public string Name { get; set; } = string.Empty;
    public int Points { get; set; }
    public List<string> Keywords { get; set; } = [];
    public List<string> Files { get; set; } = [];
}

public class FileContent
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ProjectDirectory { get; set; } = string.Empty;
}

public class ScoringResult
{
    public string FileName { get; set; } = string.Empty;
    public string Criterion { get; set; } = string.Empty;
    public int Points { get; set; }
    [JsonPropertyName("proof")]
    public string Proof { get; set; } = string.Empty;
    [JsonPropertyName("met")]
    public bool Met { get; set; }
}

// Repository operation result models
public class RepositoryCloneResult
{
    public bool Success { get; set; }
    public string? ClonedDirectory { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RepositoryScanResult
{
    public bool Success { get; set; }
    public List<FileContent>? ScannedFiles { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RubricScoringResult
{
    public bool Success { get; set; }
    public double TotalPoints { get; set; }
    public List<string> ScoringDetails { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

// Request models for bulk grading
public class CloneRepositoryRequest
{
    public string GithubUrl { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
}

public class GetRepositoryTreeRequest
{
    public string ClonedDirectory { get; set; } = string.Empty;
}

public class RemoveRepositoryItemRequest
{
    public string ClonedDirectory { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
}

public class ScoreRepositoryRequest
{
    public string ClonedDirectory { get; set; } = string.Empty;
    public string RubricJson { get; set; } = string.Empty;
}

public class GetActivityTemplateRubricRequest
{
    public string ActivityName { get; set; } = string.Empty;
}

// Enhanced Bulk Grading Request Models
public class BulkProcessingStudent
{
    public int StudentId { get; set; }
    public string RepositoryUrl { get; set; } = string.Empty;
}

public class StartBulkProcessingRequest
{
    public int ActivityTemplateId { get; set; }
    public int SectionId { get; set; }
    public List<BulkProcessingStudent> SelectedStudents { get; set; } = [];
    public bool ShowNonZeroGrades { get; set; }
}

public class UpdateApprovalRequest
{
    public string SessionId { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public bool IsApproved { get; set; }
}

public class BulkApprovalRequest
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}

public class SaveBulkGradingRequest
{
    public string SessionId { get; set; } = string.Empty;
}
