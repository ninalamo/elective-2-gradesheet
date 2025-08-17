using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Helpers;
using elective_2_gradesheet.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace elective_2_gradesheet.Services
{
    public interface IGradeService
    {
        Task ProcessAndSaveGradesAsync(CsvDisplayViewModel model);
        Task<List<Section>> GetActiveSectionsAsync();
        Task<StudentProfileViewModel> GetStudentProfileAsync(int studentId, GradingPeriod? period, string sortOrder);
        Task<PaginatedList<StudentActivityGroupViewModel>> GetStudentGroupsAsync(string searchTerm, int? sectionId, GradingPeriod? period, string sortOrder, int pageIndex, int pageSize);
        // This new method signature was added
        Task UpdateActivityAsync(int studentId, double points, double maxPoints, GradingPeriod period, string tag, string otherTag, string githubLink, string status, int? activityId, string? activityName, int? newId = 0);
        Task<int> BulkAddMissingActivitiesAsync(int studentId, GradingPeriod gradingPeriod);
    }

    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;

        // The database context is injected via the constructor.
        public GradeService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<int> BulkAddMissingActivitiesAsync(int studentId, GradingPeriod period)
        {
            var student = await _context.Students
                .Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return 0;

            // Get all unique activities (ActivityName, Period, MaxPoints, Tag) for the student's section within the specified period.
            // We include Tag here so we can use it to infer for missing activities.
            var allUniqueSectionActivities = await _context.Activities
                .Where(a => a.Student.SectionId == student.SectionId && a.Period == period)
                .Select(a => new { a.ActivityName, a.Period, a.MaxPoints, a.Tag })
                .Distinct()
                .ToListAsync();

            // Get all activities already submitted by the student for that period.
            var studentSubmittedActivityNames = await _context.Activities
                .Where(a => a.StudentId == studentId && a.Period == period)
                .Select(a => a.ActivityName)
                .ToListAsync();

            // Identify the missing activities by comparing the two lists.
            var missingActivities = allUniqueSectionActivities
                .Where(ua => !studentSubmittedActivityNames.Contains(ua.ActivityName))
                .ToList();

            var newActivities = new List<Activity>();

            foreach (var missingActivity in missingActivities)
            {
                string inferredTag = InferTagFromActivityName(missingActivity.ActivityName);
                double maxPoints = missingActivity.MaxPoints > 0 ? missingActivity.MaxPoints : 100; // Default to 100 if MaxPoints is not set or zero.

                var newActivity = new Activity
                {
                    StudentId = student.Id,
                    ActivityName = missingActivity.ActivityName, // Use the original activity name
                    MaxPoints = maxPoints,
                    Points = 0, // Default scores to zero
                    Status = "Added", // New activities are initially "Missing"
                    Period = missingActivity.Period,
                    Tag = inferredTag, // Use the inferred tag
                    GithubLink = null // Default GitHub link to null
                };
                newActivities.Add(newActivity);
            }

            _context.Activities.AddRange(newActivities);
            await _context.SaveChangesAsync();
            return newActivities.Count();
        }

        // Helper method for smart tagging based on activity name keywords
        private string InferTagFromActivityName(string activityName)
        {
            if (string.IsNullOrEmpty(activityName)) return "Other";

            string lowerName = activityName.ToLower();

            // Updated Regex for 'Assignment' patterns:
            // 1. "a" followed by one or more digits (e.g., A1, A2, A10)
            // 2. Or a word followed by underscore and then "a" and digits (e.g., prelim_a3, mid_a1)
            // 3. Or general keywords for assignments
            if (Regex.IsMatch(lowerName, @"^a\d+$") ||                      // Matches a1, a2
                Regex.IsMatch(lowerName, @"\w+_a\d+$") ||                   // Matches prelim_a3, final_a1
                lowerName.Contains("assignment") || lowerName.Contains("quiz") ||
                lowerName.Contains("exam") || lowerName.Contains("report") || lowerName.Contains("paper"))
            {
                return "Assignment";
            }

            // Regex for 'Hands-on' patterns:
            // 1. "lab" followed by a digit (e.g., lab1, lab2)
            // 2. Or "lab_" followed by a digit (e.g., lab_1, lab_2)
            // 3. Or general keywords for hands-on activities
            if (Regex.IsMatch(lowerName, @"^lab\d+$") ||
                Regex.IsMatch(lowerName, @"^lab_\d+$") ||
                lowerName.Contains("hands-on") || lowerName.Contains("project") ||
                lowerName.Contains("activity") || lowerName.Contains("practical"))
            {
                return "Hands-on";
            }

            return "Other";
        }

        public async Task<StudentProfileViewModel> GetStudentProfileAsync(int studentId, GradingPeriod? period, string sortOrder)
        {
            var student = await _context.Students
                .Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return null;

            // Get a master list of all unique activities for the student's section.
            // This query correctly selects only the properties that define a unique activity.
            var allUniqueSectionActivities = await _context.Activities
                .Where(a => a.Student.SectionId == student.SectionId)
                .Select(a => new { a.ActivityName, a.Period, a.MaxPoints })
                .Distinct()
                .ToListAsync();

            // Get all activities submitted by the current student.
            var studentActivities = await _context.Activities
                .Where(a => a.StudentId == studentId)
                .ToListAsync();

            var activitiesViewModel = new List<ActivityViewModel>();

            // Iterate through the master list of all possible activities.
            foreach (var uniqueActivity in allUniqueSectionActivities)
            {
                // Find the specific activity record for the current student.
                var submittedActivity = studentActivities.FirstOrDefault(sa => sa.ActivityName == uniqueActivity.ActivityName && sa.Period == uniqueActivity.Period);

                if (submittedActivity != null)
                {
                    // If the student has a record, add it to the view model.
                    activitiesViewModel.Add(new ActivityViewModel
                    {
                        ActivityId = submittedActivity.Id,
                        ActivityName = submittedActivity.ActivityName,
                        Tag = submittedActivity.Tag,
                        GradingPeriod = submittedActivity.Period.ToString(),
                        Status = submittedActivity.Status,
                        Points = submittedActivity.Points,
                        MaxPoints = submittedActivity.MaxPoints,
                        GithubLink = submittedActivity.GithubLink,
                        StudentId = student.Id,
                        StudentFullName = student.GetFullName(),
                        SectionName = student.Section.Name,
                    });
                }
                else
                {
                    // If no record is found, create a "Missing" entry.
                    // This ensures the activity name is still displayed.
                    activitiesViewModel.Add(new ActivityViewModel
                    {
                        ActivityId = 0, // 0 indicates a new, unsaved record.
                        ActivityName = uniqueActivity.ActivityName, // The name is retained.
                        Tag = "N/A",
                        GradingPeriod = uniqueActivity.Period.ToString(),
                        Status = "Missing",
                        Points = 0,
                        MaxPoints = uniqueActivity.MaxPoints,
                        StudentId = student.Id,
                        StudentFullName = student.GetFullName(),
                        SectionName = student.Section.Name,
                    });
                }
            }

            // Apply filtering and sorting to the final list of activities.
            if (period.HasValue)
            {
                activitiesViewModel = activitiesViewModel.Where(a => a.GradingPeriod == period.Value.ToString()).ToList();
            }

            activitiesViewModel = sortOrder switch
            {
                "period_desc" => activitiesViewModel.OrderByDescending(a => a.GradingPeriod).ToList(),
                _ => activitiesViewModel.OrderBy(a => a.GradingPeriod).ToList(),
            };

            return new StudentProfileViewModel
            {
                StudentId = student.Id,
                StudentFullName = student.GetFullName(),
                SectionName = student.Section.Name,
                Activities = activitiesViewModel,
                CurrentPeriod = period,
                CurrentSort = sortOrder,
            };
        }

        public async Task<PaginatedList<StudentActivityGroupViewModel>> GetStudentGroupsAsync(string searchTerm, int? sectionId, GradingPeriod? period, string sortOrder, int pageIndex, int pageSize)
        {
            var query = _context.Students
                .Include(s => s.Section)
                .Include(s => s.Activities)
                .AsQueryable();

            // --- Filtering ---
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s => s.FirstName.Contains(searchTerm) || s.LastName.Contains(searchTerm) || s.Email.Contains(searchTerm));
            }
            if (sectionId.HasValue)
            {
                query = query.Where(s => s.SectionId == sectionId.Value);
            }

            // --- Sorting ---
            query = sortOrder switch
            {
                "name_desc" => query.OrderByDescending(s => s.LastName),
                "Section" => query.OrderBy(s => s.Section.Name),
                "section_desc" => query.OrderByDescending(s => s.Section.Name),
                _ => query.OrderBy(s => s.LastName),
            };

            var studentGroups = query.Select(s => new
            {
                Student = s,
                FilteredActivities = s.Activities
                    .Where(a => !period.HasValue || a.Period == period.Value)
            });

            // The grouping logic is now performed in memory after fetching the data.
            var projectedGroups = studentGroups.ToList().Select(sg => new StudentActivityGroupViewModel
            {
                StudentId = sg.Student.Id,
                StudentFullName = sg.Student.GetFullName(),
                StudentNumber = sg.Student.GetStudentNumber(),
                SectionName = sg.Student.Section.Name,
                ActivitiesByPeriod = sg.FilteredActivities
                    .GroupBy(a => a.Period.ToString())
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => new ActivityViewModel
                        {
                            ActivityName = a.ActivityName,
                            GradingPeriod = a.Period.ToString(),
                            Status = a.Status,
                            Points = a.Points,
                            MaxPoints = a.MaxPoints,
                            GithubLink = a.GithubLink,
                            StudentId = sg.Student.Id,
                            StudentFullName = sg.Student.GetFullName(),
                            SectionName = sg.Student.Section.Name,
                            ActivityId = a.Id // Include the ActivityId for updates

                        }).ToList()
                    )
            });

            // Manually paginate the results
            var count = projectedGroups.Count();
            var items = projectedGroups.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PaginatedList<StudentActivityGroupViewModel>(items, count, pageIndex, pageSize);
        }

        public async Task UpdateActivityAsync(int studentId, double points, double maxPoints, GradingPeriod period, string tag, string otherTag, string githubLink, string status, int? activityId = default, string activityName = "", int? newId = 0)
        {
            if (activityId == default || activityId == 0)
            {
                // Case: Add a new activity record
                var newActivity = new Activity
                {
                    StudentId = studentId,
                    ActivityName = activityName,
                    MaxPoints = maxPoints,
                    Points = points,
                    Status = status,
                    Period = period,
                    Tag = tag == "Other" ? otherTag : tag,
                    GithubLink = githubLink
                };
                _context.Activities.Add(newActivity);
            }
            else
            {
                // Case: Update an existing activity
                var activity = await _context.Activities.FindAsync(activityId);
                if (activity != null)
                {
                    activity.Points = points;
                    activity.Period = period;
                    activity.Tag = tag == "Other" ? otherTag : tag; // Save custom tag if "Other" is selected
                    activity.GithubLink = githubLink;
                    activity.MaxPoints = maxPoints;
                    activity.Status = status;

                    // Update the activity name to reflect the new period
                    var parts = activity.ActivityName.Split(" - ");
                    activity.ActivityName = $"{period.ToString().ToUpper()} - {parts.Last()}";
                }
            }

            await _context.SaveChangesAsync();
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
