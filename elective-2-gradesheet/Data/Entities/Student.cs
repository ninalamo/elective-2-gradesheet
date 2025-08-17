using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace elective_2_gradesheet.Data.Entities
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StudentNumber { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string FirstName { get; set; }

        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        public virtual ICollection<Activity> Activities { get; set; }
    }

    public enum GradingPeriod
    {
        Prelim,
        Midterm,
        PreFinals,
        Finals
    }

    public enum Session
    {
        Online,
        Laboratory,
    }

    public class Activity
    {
        [Key]
        public int Id { get; set; }

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

        [Required]
        public string Status { get; set; }
    }
}
