using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Models;

namespace elective_2_gradesheet.Services
{
    public interface IGradeService
    {
        Task ProcessAndSaveGradesAsync(CsvDisplayViewModel model, CancellationToken ct = default);

        // Return type here should match what RecordsViewModel.StudentGroups expects.
        // If your project uses a specific paged-list type, swap it in below.
        Task<elective_2_gradesheet.Helpers.PaginatedList<StudentActivityGroupViewModel>> GetStudentGroupsAsync(
            string searchString,
            int? sectionId,
            GradingPeriod? period,
            string sortOrder,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);

        Task<List<Section>> GetActiveSectionsAsync(CancellationToken ct = default);

        // Adjust the ViewModel type name if yours differs.
        Task<StudentProfileViewModel?> GetStudentProfileAsync(
            int studentId,
            GradingPeriod? period,
            string sortOrder,
            CancellationToken ct = default);

        Task UpdateActivityAsync(
            int studentId,
            double points,
            double maxPoints,
            GradingPeriod period,
            string tag,
            string otherTag,
            string githubLink,
            string status,
            int? activityId,
            string activityName,
            int? newId,
            CancellationToken ct = default);

        Task<int> BulkAddMissingActivitiesAsync(int studentId, GradingPeriod gradingPeriod, CancellationToken ct = default);
    }
}

