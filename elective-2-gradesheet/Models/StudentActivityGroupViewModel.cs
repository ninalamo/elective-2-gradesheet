namespace elective_2_gradesheet.Models
{
    public class StudentActivityGroupViewModel
    {
        public int StudentId { get; set; }
        public string StudentFullName { get; set; }
        public string StudentNumber { get; set; }
        public string SectionName { get; set; }
        // Changed to a Dictionary to group activities by the grading period string.
        public Dictionary<string, List<ActivityViewModel>> ActivitiesByPeriod { get; set; }
    }
}
