namespace elective_2_gradesheet.Models
{
    public class ActivityViewModel
    {
        public int ActivityId { get; set; } // Added for the update functionality
        public int StudentId { get; set; }
        public string StudentFullName { get; set; }
        public string ActivityName { get; set; }
        public string GradingPeriod { get; set; }
        public string SectionName { get; set; }
        public string Status { get; set; }
        public double Points { get; set; }
        public double MaxPoints { get; set; }
        public string? Tag { get; internal set; }
        public string? GithubLink { get; set; }
    }
}
