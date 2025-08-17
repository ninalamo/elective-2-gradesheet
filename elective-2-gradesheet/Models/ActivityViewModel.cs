namespace elective_2_gradesheet.Models
{
    public class ActivityViewModel
    {
        public int StudentId { get; set; }
        public string StudentFullName { get; set; }
        public string ActivityName { get; set; }
        public string GradingPeriod { get; set; }
        public string SectionName { get; set; }
        public string Status { get; set; }
        public double Points { get; set; }
        public double MaxPoints { get; set; }
    }
}
