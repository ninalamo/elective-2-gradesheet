// Models/RecordsViewModel.cs
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

// Ensure this namespace matches your project
namespace elective_2_gradesheet.Models
{
    public class RecordsViewModel
    {
        // This property holds the paginated list of student groups for the view.
        // The error occurs if this property is missing or named differently.
        public PaginatedList<StudentActivityGroupViewModel> StudentGroups { get; set; }

        public IEnumerable<SelectListItem> Sections { get; set; }
        public string CurrentSearch { get; set; }
        public int? CurrentSectionId { get; set; }
        public GradingPeriod? CurrentPeriod { get; set; }
        public string CurrentSort { get; set; }
    }
}
