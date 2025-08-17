using CsvHelper.Configuration.Attributes;
using System.ComponentModel;

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
}
