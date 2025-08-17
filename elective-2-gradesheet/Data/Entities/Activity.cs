using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace elective_2_gradesheet.Data.Entities
{
    public enum ActivityTag
    {
        Assignment,
        HandsOn,
        Other
    }

    public class Activity
    {
        [Key]
        public int Id { get; set; }

        public string? Tag { get; set; } = string.Empty;

        [Required]
        public GradingPeriod Period { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [Required]
        public string ActivityName { get; set; }

        public double MaxPoints { get; set; }

        public double Points { get; set; }
        public string? GithubLink { get; set; } = string.Empty;
       

        [Required]
        public string Status { get; set; }
    }
}
