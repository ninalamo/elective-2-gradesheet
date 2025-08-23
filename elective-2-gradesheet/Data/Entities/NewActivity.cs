using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace elective_2_gradesheet.Data.Entities
{
    /// <summary>
    /// Represents an activity template/definition that can be assigned to students
    /// </summary>
    public class NewActivity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section Section { get; set; }

        [Required]
        [MaxLength(20)]
        public string SchoolYear { get; set; }

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
        /// When this activity was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this activity was last updated
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this activity is currently active/assignable
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<StudentActivity> StudentActivities { get; set; } = new List<StudentActivity>();
        
        // Optional: If you want a separate Rubric table as well
        public virtual ICollection<Rubric> Rubrics { get; set; } = new List<Rubric>();
    }
}
