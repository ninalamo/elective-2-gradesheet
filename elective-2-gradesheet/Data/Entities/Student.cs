using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace elective_2_gradesheet.Data.Entities
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        public string GetStudentNumber()  => Email?.Split('@')[0] ?? string.Empty;

        [Required]
        public string? LastName { get; set; }

        [Required]
        public string? FirstName { get; set; }

        public string GetFullName() => $"{LastName}, {FirstName}";

        [Required]
        public string? Email { get; set; }

        // Foreign key for the Section
        [Required]
        public int SectionId { get; set; }

        [ForeignKey("SectionId")]
        public virtual Section? Section { get; set; }

        public virtual ICollection<StudentSubmission> Activities { get; set; } = [];
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
}
