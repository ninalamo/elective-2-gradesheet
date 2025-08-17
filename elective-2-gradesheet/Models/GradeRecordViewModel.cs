using CsvHelper.Configuration.Attributes;
using elective_2_gradesheet.Data.Entities;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace elective_2_gradesheet.Models
{
    public class GradeRecordViewModel
    {
        [Name("Last Name")]
        public string LastName { get; set; }

        [Name("First Name")]
        public string FirstName { get; set; }

        [Name("Email Address")]
        public string Email { get; set; }

        [Name("Assignments")]
        public string ActivityName { get; set; }

        [Name("Status")]
        public string Status { get; set; }

        [Name("Points")]
        public string Points { get; set; }

        [Name("Max Points")]
        public string MaxPoints { get; set; }

    }

    public class CsvDisplayViewModel
    {
        public List<GradeRecordViewModel> GradeRecords { get; set; }
        public GradingPeriod GradingPeriod { get; set; } = GradingPeriod.Prelim;

        [Required]
        public int? SectionId { get; set; } = default!;

        public CsvDisplayViewModel()
        {
            GradeRecords = new List<GradeRecordViewModel>();
        }
    }

    public class ActivityViewModel
    {
        public string StudentFullName { get; set; }
        public string ActivityName { get; set; }
        public string GradingPeriod { get; set; }
        public string Status { get; set; }
        public double Points { get; set; }
        public double MaxPoints { get; set; }
    }
}
