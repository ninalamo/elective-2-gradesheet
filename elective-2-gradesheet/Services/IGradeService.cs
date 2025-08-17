using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Helpers;
using elective_2_gradesheet.Models;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Services
{
    public interface IGradeService
    {
        Task<PaginatedList<ActivityViewModel>> GetActivitiesAsync(string searchTerm, int pageIndex, int pageSize);

        Task ProcessAndSaveGradesAsync(CsvDisplayViewModel model);
    }

    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;

        // The database context is injected via the constructor.
        public GradeService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<PaginatedList<ActivityViewModel>> GetActivitiesAsync(string searchTerm, int pageIndex, int pageSize)
        {
            var query = _context.Activities.Include(a => a.Student).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                // This is the corrected filtering logic.
                // Instead of converting the enum to a string in the database,
                // we check the search term against the known string values of the enum.
                // This approach is fully translatable to SQL.
                query = query.Where(a =>
                    a.Student.FirstName.Contains(searchTerm) ||
                    a.Student.LastName.Contains(searchTerm) ||
                    a.ActivityName.Contains(searchTerm) ||
                    (a.Period == GradingPeriod.Prelim && "PRELIM".Contains(searchTerm.ToUpper())) ||
                    (a.Period == GradingPeriod.Midterm && "MIDTERM".Contains(searchTerm.ToUpper())) ||
                    (a.Period == GradingPeriod.PreFinals && "PREFINAL".Contains(searchTerm.ToUpper())) ||
                    (a.Period == GradingPeriod.Finals && "FINALS".Contains(searchTerm.ToUpper()))
                );
            }

            var activityViewModels = query.Select(a => new ActivityViewModel
            {
                StudentFullName = a.Student.GetFullName(),
                ActivityName = a.ActivityName,
                Status = a.Status,
                Points = a.Points,
                MaxPoints = a.MaxPoints,
                GradingPeriod = a.Period.ToString() // ToString() is safe here because it runs after the data is fetched
            });

            return await PaginatedList<ActivityViewModel>.CreateAsync(activityViewModels.AsNoTracking(), pageIndex, pageSize);
        }

        public async Task ProcessAndSaveGradesAsync(CsvDisplayViewModel model)
        {
            var records = model.GradeRecords;
            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Email)) continue;

                var studentNumber = record.Email.Split('@')[0];

                // Find an existing student or create a new one.
                var student = await _context.Students
                    .AsNoTracking() // Use AsNoTracking for read-only operations
                   .FirstOrDefaultAsync(s => s.Email == record.Email);
                   

                if (student == null)
                {
                    student = new Student
                    {
                        LastName = record.LastName,
                        FirstName = record.FirstName,
                        Email = record.Email,
                        SectionId = model.SectionId.Value // Ensure SectionId is set from the model
                    };
                    _context.Students.Add(student);
                    // We save here to ensure the student has an ID before we create the activity.
                    await _context.SaveChangesAsync();
                }

                // Apply scoring rules.
                var points = double.TryParse(record.Points, out var p) ? p : 0;
                if (record.Status != "Turned in")
                {
                    points = 0;
                }

                // Create the new activity record.
                if(_context.Activities.Any(a => a.StudentId == student.Id && a.ActivityName == record.ActivityName && a.Period == model.GradingPeriod))
                {
                    // If the activity already exists, we can skip creating it again.
                    continue;
                }
                var activity = new Activity
                {
                    StudentId = student.Id,
                    ActivityName = $"{record.ActivityName}",
                    MaxPoints = double.TryParse(record.MaxPoints, out var mp) ? mp : 0,
                    Points = points,
                    Status = record.Status,
                    Period = model.GradingPeriod
                };
                _context.Activities.Add(activity);
            }

            // Save all the newly created activities in a single transaction.
            await _context.SaveChangesAsync();
        }
    }
}
