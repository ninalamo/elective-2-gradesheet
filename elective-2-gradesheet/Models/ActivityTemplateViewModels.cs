using System.ComponentModel.DataAnnotations;
using elective_2_gradesheet.Data.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace elective_2_gradesheet.Models
{
    public class ActivityTemplateListViewModel
    {
        public IEnumerable<ActivityTemplate> ActivityTemplates { get; set; } = new List<ActivityTemplate>();
        public IEnumerable<SelectListItem> Sections { get; set; } = new List<SelectListItem>();
        public string? CurrentSearch { get; set; }
        public int? CurrentSectionId { get; set; }
        public GradingPeriod? CurrentPeriod { get; set; }
        public string? CurrentSort { get; set; }
    }

    public class ActivityTemplateCreateViewModel
    {
        [Required]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Section")]
        public int SectionId { get; set; }

        [Required]
        [Display(Name = "Grading Period")]
        public GradingPeriod Period { get; set; }

        [Required]
        [Range(0.1, 1000, ErrorMessage = "Max points must be between 0.1 and 1000.")]
        [Display(Name = "Maximum Points")]
        public double MaxPoints { get; set; } = 100;

        [StringLength(50, ErrorMessage = "Tag cannot exceed 50 characters.")]
        public string? Tag { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Display(Name = "Rubric JSON")]
        public string? RubricJson { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class ActivityTemplateEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Section")]
        public int SectionId { get; set; }

        [Required]
        [Display(Name = "Grading Period")]
        public GradingPeriod Period { get; set; }

        [Required]
        [Range(0.1, 1000, ErrorMessage = "Max points must be between 0.1 and 1000.")]
        [Display(Name = "Maximum Points")]
        public double MaxPoints { get; set; }

        [StringLength(50, ErrorMessage = "Tag cannot exceed 50 characters.")]
        public string? Tag { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Display(Name = "Rubric JSON")]
        public string? RubricJson { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }

    public class RubricEditorViewModel
    {
        public int ActivityTemplateId { get; set; }
        
        [Display(Name = "Activity Template")]
        public string ActivityTemplateName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Rubric JSON")]
        public string RubricJson { get; set; } = "[]";
    }

    public class RubricItemViewModel
    {
        [Required]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 100, ErrorMessage = "Points must be between 1 and 100.")]
        public int Points { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one keyword is required.")]
        public List<string> Keywords { get; set; } = new List<string>();

        [Required]
        [MinLength(1, ErrorMessage = "At least one file pattern is required.")]
        public List<string> Files { get; set; } = new List<string>();
    }

    public class RubricValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int TotalPoints { get; set; }
        public List<RubricItemViewModel> RubricItems { get; set; } = new List<RubricItemViewModel>();
    }

    public class ActivityTemplateDuplicateViewModel
    {
        public int SourceId { get; set; }
        
        [Required]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;
        
        [Display(Name = "Copy Rubric")]
        public bool CopyRubric { get; set; } = true;
        
        [Display(Name = "Copy to Different Section")]
        public bool CopyToDifferentSection { get; set; } = false;
        
        [Display(Name = "Target Section")]
        public int? TargetSectionId { get; set; }
    }

    public class ActivityTemplateSearchViewModel
    {
        public string? SearchTerm { get; set; }
        public int? SectionId { get; set; }
        public GradingPeriod? Period { get; set; }
        public bool? HasRubric { get; set; }
        public bool? IsActive { get; set; } = true;
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; } = "asc";
    }

    public class ActivityTemplateStatsViewModel
    {
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public int TemplatesWithRubrics { get; set; }
        public int TemplatesWithoutRubrics { get; set; }
        public Dictionary<GradingPeriod, int> TemplatesByPeriod { get; set; } = new Dictionary<GradingPeriod, int>();
        public Dictionary<string, int> TemplatesBySection { get; set; } = new Dictionary<string, int>();
    }

    public class RubricPreviewViewModel
    {
        public string ActivityTemplateName { get; set; } = string.Empty;
        public List<RubricItemViewModel> RubricItems { get; set; } = new List<RubricItemViewModel>();
        public int TotalPoints { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; }
    }
}
