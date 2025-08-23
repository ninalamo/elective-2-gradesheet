using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace elective_2_gradesheet.Data.Entities
{
    /// <summary>
    /// Represents a detailed rubric for an activity (optional - can also store rubric in NewActivity.RubricJson)
    /// </summary>
    public class Rubric
    {
        [Key]
        public int RubricId { get; set; }

        [Required]
        public int ActivityId { get; set; }

        [ForeignKey("ActivityId")]
        public virtual NewActivity Activity { get; set; }

        /// <summary>
        /// Name/Title of the rubric
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        /// <summary>
        /// JSON string containing the complete rubric structure
        /// Example structure:
        /// {
        ///   "criteria": [
        ///     {
        ///       "name": "Code Quality",
        ///       "weight": 0.4,
        ///       "levels": [
        ///         {"score": 4, "description": "Excellent code quality"},
        ///         {"score": 3, "description": "Good code quality"},
        ///         {"score": 2, "description": "Fair code quality"},
        ///         {"score": 1, "description": "Poor code quality"}
        ///       ]
        ///     }
        ///   ],
        ///   "totalPoints": 100
        /// }
        /// </summary>
        [Required]
        public string RubricJson { get; set; }

        /// <summary>
        /// Version of the rubric for tracking changes
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Whether this rubric is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When this rubric was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this rubric was last updated
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created this rubric (could be teacher email or ID)
        /// </summary>
        [MaxLength(100)]
        public string? CreatedBy { get; set; }
    }
}
