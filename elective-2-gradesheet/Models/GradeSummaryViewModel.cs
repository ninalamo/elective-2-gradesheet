using elective_2_gradesheet.Data.Entities;

namespace elective_2_gradesheet.Models
{
    public class GradeSummaryViewModel
    {
        public List<SectionSummary> Sections { get; set; } = new List<SectionSummary>();
        public int TotalStudents { get; set; }
        public int TotalActivities { get; set; }
        public GradingPeriod? FilterPeriod { get; set; }
        public int? FilterSectionId { get; set; }
    }

    public class SectionSummary
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public string SchoolYear { get; set; }
        public List<StudentGradeSummary> Students { get; set; } = new List<StudentGradeSummary>();
        public List<string> ActivityNames { get; set; } = new List<string>();
        public double SectionAverage { get; set; }
    }

    public class StudentGradeSummary
    {
        public int StudentId { get; set; }
        public string StudentNumber { get; set; }
        public string StudentFullName { get; set; }
        public string SectionName { get; set; }
        public List<ActivityGradeSummary> Activities { get; set; } = new List<ActivityGradeSummary>();
        public double TotalPoints { get; set; }
        public double TotalMaxPoints { get; set; }
        public double OverallPercentage { get; set; }
        public string OverallGrade { get; set; }
    }

    public class ActivityGradeSummary
    {
        public int ActivityId { get; set; }
        public string ActivityName { get; set; }
        public double Points { get; set; }
        public double MaxPoints { get; set; }
        public double Percentage { get; set; }
        public string Status { get; set; }
        public GradingPeriod Period { get; set; }
        public string Tag { get; set; }
    }
}
