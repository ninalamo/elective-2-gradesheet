using elective_2_gradesheet.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace elective_2_gradesheet.Models
{
    public class CsvDisplayViewModel
    {
        public List<GradeRecordViewModel> GradeRecords { get; set; }
        public GradingPeriod GradingPeriod { get; set; } = GradingPeriod.Prelim;
        public string? Tag { get; set; }

        [Required]
        public int? SectionId { get; set; } = default!;

        public CsvDisplayViewModel()
        {
            GradeRecords = new List<GradeRecordViewModel>();
        }
    }
}
