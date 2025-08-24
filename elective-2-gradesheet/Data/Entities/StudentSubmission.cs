using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace elective_2_gradesheet.Data.Entities
{
    /// <summary>
    /// Represents a student's submission/grade for a specific activity template
    /// </summary>
    public class StudentSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [Required]
        public int ActivityTemplateId { get; set; }

        [ForeignKey(nameof(ActivityTemplateId))]
        public virtual ActivityTemplate ActivityTemplate { get; set; } = null!;

        /// <summary>
        /// Points earned by the student (0 to ActivityTemplate.MaxPoints)
        /// </summary>
        public double Points { get; set; }

        /// <summary>
        /// Status of the student's submission
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Missing"; // Missing, Submitted, Graded, Late, etc.

        /// <summary>
        /// Link to the student's GitHub repository or submission
        /// </summary>
        [MaxLength(500)]
        public string? GithubLink { get; set; }

        /// <summary>
        /// When the student submitted this activity
        /// </summary>
        public DateTime? SubmissionDate { get; set; }

        /// <summary>
        /// When this was graded
        /// </summary>
        public DateTime? GradedDate { get; set; }

        /// <summary>
        /// Additional notes or feedback for this student's submission
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// JSON string containing rubric scores if using detailed rubric scoring
        /// </summary>
        public string? RubricScoreJson { get; set; }

        /// <summary>
        /// When this record was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this record was last updated
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
