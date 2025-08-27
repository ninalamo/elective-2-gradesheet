using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Models;
using elective_2_gradesheet.Helpers;

namespace elective_2_gradesheet.Services
{
    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _db;

        public GradeService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Task ProcessAndSaveGradesAsync(CsvDisplayViewModel model, CancellationToken ct = default)
        {
            // TODO: replace with your real import logic.
            // This stub exists so the app compiles and runs.
            return Task.CompletedTask;
        }

        public Task<PaginatedList<StudentActivityGroupViewModel>> GetStudentGroupsAsync(
            string searchString,
            int? sectionId,
            GradingPeriod? period,
            string sortOrder,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
        {
            // TODO: Replace with your real query + paging that returns the
            // exact type your Records view expects.
            // For now we return an empty PaginatedList so the controller compiles.
            var emptyList = new PaginatedList<StudentActivityGroupViewModel>(new List<StudentActivityGroupViewModel>(), 0, pageNumber, pageSize);
            return Task.FromResult(emptyList);
        }

        public async Task<List<Section>> GetActiveSectionsAsync(CancellationToken ct = default)
        {
            // Assuming a boolean IsActive on Section; tweak if your schema differs.
            return await Task.FromResult(_db.Sections.Where(s => s.IsActive).ToList());
        }

        public Task<StudentProfileViewModel?> GetStudentProfileAsync(
            int studentId,
            GradingPeriod? period,
            string sortOrder,
            CancellationToken ct = default)
        {
            // TODO: Map from your entities to StudentProfileViewModel.
            StudentProfileViewModel? vm = null;
            return Task.FromResult(vm);
        }

        public Task UpdateActivityAsync(
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
            CancellationToken ct = default)
        {
            // TODO: Implement update/insert logic.
            return Task.CompletedTask;
        }

        public Task<int> BulkAddMissingActivitiesAsync(int studentId, GradingPeriod gradingPeriod, CancellationToken ct = default)
        {
            // TODO: Implement bulk add logic; return number added.
            return Task.FromResult(0);
        }
    }
}
