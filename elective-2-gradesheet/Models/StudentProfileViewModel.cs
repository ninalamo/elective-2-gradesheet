using elective_2_gradesheet.Data.Entities;

namespace elective_2_gradesheet.Models
{
    public class StudentProfileViewModel
    {
        public int StudentId { get; set; }
        public string StudentFullName { get; set; }
        public string SectionName { get; set; }
        public GradingPeriod? CurrentPeriod { get; set; }
        public string CurrentSort { get; set; }
        public List<ActivityViewModel> Activities { get; set; }
    }
}
