using System.ComponentModel.DataAnnotations;


namespace elective_2_gradesheet.Data.Entities
{
    public class Section
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string SchoolYear { get; set; } = "2025-2026";

        public bool IsActive { get; set; } = true;

        // A section can have many students
        public virtual ICollection<Student> Students { get; set; } = [];
    }
}
