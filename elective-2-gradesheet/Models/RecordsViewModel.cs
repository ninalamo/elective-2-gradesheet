using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace elective_2_gradesheet.Models
{
    public class RecordsViewModel
    {
        public PaginatedList<ActivityViewModel> Activities { get; set; }
        public IEnumerable<SelectListItem> Sections { get; set; }
        public string CurrentSearch { get; set; }
        public int? CurrentSectionId { get; set; }
        public GradingPeriod? CurrentPeriod { get; set; }
        public string CurrentSort { get; set; }
    }
}
