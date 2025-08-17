using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Helpers;
using elective_2_gradesheet.Models;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Services
{
    public interface IGradeService
    {
        Task<PaginatedList<ActivityViewModel>> GetActivitiesAsync(string searchTerm, int? sectionId, GradingPeriod? period, string sortOrder, int pageIndex, int pageSize);

        Task ProcessAndSaveGradesAsync(CsvDisplayViewModel model);
        Task<List<Section>> GetActiveSectionsAsync();
        Task<StudentProfileViewModel> GetStudentProfileAsync(int studentId, GradingPeriod? period, string sortOrder);
    }

    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;

        // The database context is injected via the constructor.
        public GradeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudentProfileViewModel> GetStudentProfileAsync(int studentId, GradingPeriod? period, string sortOrder)
        {
            var student = await _context.Students
                .Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return null;

            var query = _context.Activities
                .Where(a => a.StudentId == studentId)
                .AsQueryable();

            if (period.HasValue)
            {
                query = query.Where(a => a.Period == period.Value);
            }

            query = sortOrder switch
            {
                "period_desc" => query.OrderByDescending(a => a.Period),
                _ => query.OrderBy(a => a.Period),
            };

            var activities = await query.Select(a => new ActivityViewModel
            {
                ActivityName = a.ActivityName,
                GradingPeriod = a.Period.ToString(),
                Status = a.Status,
                Points = a.Points,
                MaxPoints = a.MaxPoints
            }).ToListAsync();

            // The calculation logic is now here, inside the service.
            var totalPoints = activities.Sum(a => a.Points);
            var totalMaxPoints = activities.Sum(a => a.MaxPoints);

            return new StudentProfileViewModel
            {
                StudentId = student.Id,
                StudentFullName = student.GetFullName(),
                SectionName = student.Section.Name,
                Activities = activities,
                CurrentPeriod = period,
                CurrentSort = sortOrder,
              
            };
        }

        public async Task<PaginatedList<ActivityViewModel>> GetActivitiesAsync(string searchTerm, int? sectionId, GradingPeriod? period, string sortOrder, int pageIndex, int pageSize)
        {
            var query = _context.Activities
                .Include(a => a.Student)
                .ThenInclude(s => s.Section)
                .AsQueryable();

            // --- Filtering ---
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(a => a.Student.FirstName.Contains(searchTerm) ||
                                       a.Student.LastName.Contains(searchTerm) ||
                                       a.ActivityName.Contains(searchTerm));
            }

            if (sectionId.HasValue)
            {
                query = query.Where(a => a.Student.SectionId == sectionId.Value);
            }

            if (period.HasValue)
            {
                query = query.Where(a => a.Period == period.Value);
            }

            // --- Sorting ---
            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(a => a.Student.LastName),
                "Section" => query.OrderBy(a => a.Student.Section.Name),
                "section_desc" => query.OrderByDescending(a => a.Student.Section.Name),
                _ => query.OrderBy(a => a.Student.LastName),// Default sort
            };

            var activityViewModels = query.Select(a => new ActivityViewModel
            {
                StudentId = a.Student.Id,
                StudentFullName = a.Student.GetFullName(),
                ActivityName = a.ActivityName,
                Status = a.Status,
                Points = a.Points,
                MaxPoints = a.MaxPoints,
                GradingPeriod = a.Period.ToString(),
                SectionName = a.Student.Section.Name
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
                    Period = model.GradingPeriod,
                    Tag = model.Tag ?? string.Empty // Use the tag from the model or default to empty
                };
                _context.Activities.Add(activity);
            }

            // Save all the newly created activities in a single transaction.
            await _context.SaveChangesAsync();
        }

        public async Task<List<Section>> GetActiveSectionsAsync()
        {
            return await _context.Sections.Where(s => s.IsActive).ToListAsync();
        }
    }
}
