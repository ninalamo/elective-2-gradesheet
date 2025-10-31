using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace elective_2_gradesheet.Data.Entities
{
    /// <summary>
    /// Represents an activity template that can be assigned to students
    /// </summary>
    public class ActivityTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int SectionId { get; set; }

        [ForeignKey(nameof(SectionId))]
        public virtual Section Section { get; set; } = null!;

        [Required]
        public GradingPeriod Period { get; set; }

        [Required]
        public double MaxPoints { get; set; }

        [MaxLength(50)]
        public string? Tag { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// JSON string containing the rubric criteria and scoring guide
        /// </summary>
        public string? RubricJson { get; set; }

        /// <summary>
        /// When this activity template was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this activity template was last updated
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this activity template is currently active/assignable
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<StudentSubmission> StudentSubmissions { get; set; } = new List<StudentSubmission>();
    }
}
