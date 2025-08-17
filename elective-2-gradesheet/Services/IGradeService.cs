using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Models;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Services
{
    public interface IGradeService
    {
        Task ProcessAndSaveGradesAsync(IEnumerable<GradeRecordViewModel> records, GradingPeriod gradingPeriod);
    }

    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;

        // The database context is injected via the constructor.
        public GradeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcessAndSaveGradesAsync(IEnumerable<GradeRecordViewModel> records, GradingPeriod gradingPeriod)
        {
            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Email)) continue;

                var studentNumber = record.Email.Split('@')[0];

                // Find an existing student or create a new one.
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

                if (student == null)
                {
                    student = new Student
                    {
                        LastName = record.LastName,
                        FirstName = record.FirstName,
                        Email = record.Email,
                        StudentNumber = studentNumber,
                        FullName = $"{record.FirstName} {record.LastName}"
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
                if(_context.Activities.Any(a => a.StudentId == student.Id && a.ActivityName == record.ActivityName && a.Period == gradingPeriod))
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
                    Period = gradingPeriod
                };
                _context.Activities.Add(activity);
            }

            // Save all the newly created activities in a single transaction.
            await _context.SaveChangesAsync();
        }
    }
}
