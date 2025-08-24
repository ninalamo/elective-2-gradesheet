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

        Task<(bool success, string message, int? studentId)> GetNextStudentAsync(int currentStudentId, int? sectionId = null, string activityName = null, bool includeChecked = false);

        Task<(bool success, string message, string rubricJson)> GetActivityTemplateRubricAsync(string activityName);
    }

    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;

        // The database context is injected via the constructor.
        public GradeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, string message, string rubricJson)> GetActivityTemplateRubricAsync(string activityName)
        {
            try
            {
                var activityTemplate = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Name == activityName && at.IsActive);

                if (activityTemplate != null && !string.IsNullOrEmpty(activityTemplate.RubricJson))
                {
                    return (true, null, activityTemplate.RubricJson);
                }
                else
                {
                    return (false, "No rubric found for this activity.", null);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error getting rubric: {ex.Message}", null);
            }
        }

        public async Task<int> BulkAddMissingActivitiesAsync(int studentId, GradingPeriod period)
        {
            var student = await _context.Students
                .Include(s => s.Section)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return 0;

            // Get all activity templates for the student's section within the specified period.
            var allSectionActivityTemplates = await _context.ActivityTemplates
                .Where(a => a.SectionId == student.SectionId && a.Period == period && a.IsActive)
                .ToListAsync();

            // Get all student submissions already created for this student in that period.
            var studentSubmissions = await _context.StudentSubmissions
                .Include(ss => ss.ActivityTemplate)
                .Where(ss => ss.StudentId == studentId && ss.ActivityTemplate.Period == period)
                .Select(ss => ss.ActivityTemplateId)
                .ToListAsync();

            // Identify the missing activity templates (no submission exists for them).
            var missingTemplates = allSectionActivityTemplates
                .Where(template => !studentSubmissions.Contains(template.Id))
                .ToList();

            var newSubmissions = new List<StudentSubmission>();

            foreach (var template in missingTemplates)
            {
                var newSubmission = new StudentSubmission
                {
                    StudentId = student.Id,
                    ActivityTemplateId = template.Id,
                    Points = 0, // Default scores to zero
                    Status = "Missing", // New submissions are initially "Missing"
                    GithubLink = null, // Default GitHub link to null
                    SubmissionDate = null,
                    GradedDate = null
                };
                newSubmissions.Add(newSubmission);
            }

            _context.StudentSubmissions.AddRange(newSubmissions);
            await _context.SaveChangesAsync();
            return newSubmissions.Count;
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

            // Get all activity templates for the student's section.
            var allSectionActivityTemplates = await _context.ActivityTemplates
                .Where(a => a.SectionId == student.SectionId && a.IsActive)
                .ToListAsync();

            // Get all student submissions for the current student.
            var studentSubmissions = await _context.StudentSubmissions
                .Include(ss => ss.ActivityTemplate)
                .Where(ss => ss.StudentId == studentId)
                .ToListAsync();

            var activitiesViewModel = new List<ActivityViewModel>();

            // Iterate through all activity templates in the section.
            foreach (var template in allSectionActivityTemplates)
            {
                // Find the specific submission record for the current student.
                var submission = studentSubmissions.FirstOrDefault(ss => ss.ActivityTemplateId == template.Id);

                if (submission != null)
                {
                    // If the student has a submission, add it to the view model.
                    activitiesViewModel.Add(new ActivityViewModel
                    {
                        ActivityId = submission.Id,
                        ActivityName = template.Name,
                        Tag = template.Tag ?? "N/A",
                        GradingPeriod = template.Period.ToString(),
                        Status = submission.Status,
                        Points = submission.Points,
                        MaxPoints = template.MaxPoints,
                        GithubLink = submission.GithubLink,
                        StudentId = student.Id,
                        StudentFullName = student.GetFullName(),
                        SectionName = student.Section.Name,
                    });
                }
                else
                {
                    // If no submission is found, create a "Missing" entry.
                    activitiesViewModel.Add(new ActivityViewModel
                    {
                        ActivityId = 0, // 0 indicates a new, unsaved record.
                        ActivityName = template.Name,
                        Tag = template.Tag ?? "N/A",
                        GradingPeriod = template.Period.ToString(),
                        Status = "Missing",
                        Points = 0,
                        MaxPoints = template.MaxPoints,
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
                    .ThenInclude(ss => ss.ActivityTemplate)
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
                    .Where(a => !period.HasValue || a.ActivityTemplate.Period == period.Value)
            });

            // The grouping logic is now performed in memory after fetching the data.
            var projectedGroups = studentGroups.ToList().Select(sg => new StudentActivityGroupViewModel
            {
                StudentId = sg.Student.Id,
                StudentFullName = sg.Student.GetFullName(),
                StudentNumber = sg.Student.GetStudentNumber(),
                SectionName = sg.Student.Section.Name,
                ActivitiesByPeriod = sg.FilteredActivities
                    .GroupBy(a => a.ActivityTemplate.Period.ToString())
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => new ActivityViewModel
                        {
                            ActivityName = a.ActivityTemplate.Name,
                            GradingPeriod = a.ActivityTemplate.Period.ToString(),
                            Status = a.Status,
                            Points = a.Points,
                            MaxPoints = a.ActivityTemplate.MaxPoints,
                            GithubLink = a.GithubLink,
                            Tag = a.ActivityTemplate.Tag ?? "N/A",
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
                // Case: Add a new submission record
                // This case is complex because we need to either find or create an ActivityTemplate
                // and then create a StudentSubmission

                var student = await _context.Students.FindAsync(studentId);
                if (student == null) return;

                // Try to find an existing activity template with the same name and period
                var activityTemplate = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Name == activityName &&
                                             at.Period == period &&
                                             at.SectionId == student.SectionId);

                if (activityTemplate == null)
                {
                    // Create a new activity template
                    activityTemplate = new ActivityTemplate
                    {
                        Name = activityName,
                        SectionId = student.SectionId,
                        Period = period,
                        MaxPoints = maxPoints,
                        Tag = tag == "Other" ? otherTag : tag,
                        Description = "",
                        RubricJson = null,
                        IsActive = true
                    };
                    _context.ActivityTemplates.Add(activityTemplate);
                    await _context.SaveChangesAsync(); // Save to get the ID
                }
                else
                {
                    // Update the existing template's max points if different
                    if (activityTemplate.MaxPoints != maxPoints)
                    {
                        activityTemplate.MaxPoints = maxPoints;
                        activityTemplate.UpdatedDate = DateTime.UtcNow;
                    }
                }

                // Create the student submission
                var newSubmission = new StudentSubmission
                {
                    StudentId = studentId,
                    ActivityTemplateId = activityTemplate.Id,
                    Points = points,
                    Status = status,
                    GithubLink = githubLink,
                    SubmissionDate = status == "Submitted" ? DateTime.UtcNow : null,
                    GradedDate = null
                };
                _context.StudentSubmissions.Add(newSubmission);
            }
            else
            {
                // Case: Update an existing student submission
                var submission = await _context.StudentSubmissions
                    .Include(ss => ss.ActivityTemplate)
                    .FirstOrDefaultAsync(ss => ss.Id == activityId);

                if (submission != null)
                {
                    submission.Points = points;
                    submission.Status = status;
                    submission.GithubLink = githubLink;
                    submission.UpdatedDate = DateTime.UtcNow;

                    if (status == "Submitted" && submission.SubmissionDate == null)
                    {
                        submission.SubmissionDate = DateTime.UtcNow;
                    }

                    // Update the activity template's max points if different
                    if (submission.ActivityTemplate.MaxPoints != maxPoints)
                    {
                        submission.ActivityTemplate.MaxPoints = maxPoints;
                        submission.ActivityTemplate.UpdatedDate = DateTime.UtcNow;
                    }

                    // Update the tag if it's different (tag is stored in ActivityTemplate)
                    var newTag = tag == "Other" ? otherTag : tag;
                    if (submission.ActivityTemplate.Tag != newTag)
                    {
                        submission.ActivityTemplate.Tag = newTag;
                        submission.ActivityTemplate.UpdatedDate = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task ProcessAndSaveGradesAsync(CsvDisplayViewModel model)
        {
            var records = model.GradeRecords;
            var sectionId = model.SectionId.Value;
            var gradingPeriod = model.GradingPeriod;
            var tag = model.Tag ?? "Other";

            // Group records by activity name to reduce database calls
            var recordsByActivity = records.GroupBy(r => r.ActivityName).ToList();

            foreach (var activityGroup in recordsByActivity)
            {
                var activityName = activityGroup.Key;
                if (string.IsNullOrEmpty(activityName)) continue;

                // Find or create the activity template for this activity
                var activityTemplate = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Name == activityName &&
                                             at.Period == gradingPeriod &&
                                             at.SectionId == sectionId);

                if (activityTemplate == null)
                {
                    // Create a new activity template
                    var maxPoints = activityGroup.FirstOrDefault()?.MaxPoints;
                    var parsedMaxPoints = double.TryParse(maxPoints, out var mp) ? mp : 100.0;

                    activityTemplate = new ActivityTemplate
                    {
                        Name = activityName,
                        SectionId = sectionId,
                        Period = gradingPeriod,
                        MaxPoints = parsedMaxPoints,
                        Tag = InferTagFromActivityName(activityName), // Use the smart tagging helper
                        Description = $"Activity imported from CSV: {activityName}",
                        RubricJson = null,
                        IsActive = true
                    };
                    _context.ActivityTemplates.Add(activityTemplate);
                    await _context.SaveChangesAsync(); // Save to get ID
                }

                // Process each student record for this activity
                foreach (var record in activityGroup)
                {
                    if (string.IsNullOrEmpty(record.Email)) continue;

                    // Find an existing student or create a new one.
                    var student = await _context.Students
                        .FirstOrDefaultAsync(s => s.Email == record.Email);

                    if (student == null)
                    {
                        student = new Student
                        {
                            LastName = record.LastName,
                            FirstName = record.FirstName,
                            Email = record.Email,
                            SectionId = sectionId
                        };
                        _context.Students.Add(student);
                        await _context.SaveChangesAsync(); // Save to get ID
                    }

                    // Check if submission already exists
                    var existingSubmission = await _context.StudentSubmissions
                        .FirstOrDefaultAsync(ss => ss.StudentId == student.Id &&
                                                 ss.ActivityTemplateId == activityTemplate.Id);

                    if (existingSubmission != null)
                    {
                        // Update existing submission
                        var points = double.TryParse(record.Points, out var p) ? p : 0;
                        if (record.Status != "Turned in")
                        {
                            points = 0;
                        }

                        existingSubmission.Points = points;
                        existingSubmission.Status = record.Status;
                        existingSubmission.UpdatedDate = DateTime.UtcNow;

                        if (record.Status == "Turned in" && existingSubmission.SubmissionDate == null)
                        {
                            existingSubmission.SubmissionDate = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Create new submission
                        var points = double.TryParse(record.Points, out var p) ? p : 0;
                        if (record.Status != "Turned in")
                        {
                            points = 0;
                        }

                        var submission = new StudentSubmission
                        {
                            StudentId = student.Id,
                            ActivityTemplateId = activityTemplate.Id,
                            Points = points,
                            Status = record.Status,
                            GithubLink = null,
                            SubmissionDate = record.Status == "Turned in" ? DateTime.UtcNow : null,
                            GradedDate = null
                        };
                        _context.StudentSubmissions.Add(submission);
                    }
                }

                // Save all changes in a single transaction.
                await _context.SaveChangesAsync();
            }

        }
        public async Task<List<Section>> GetActiveSectionsAsync()
        {
            return await _context.Sections.Where(s => s.IsActive).ToListAsync();
        }

        public async Task<(bool success, string message, int? studentId)> GetNextStudentAsync(
      int currentStudentId,
      int? sectionId = null,
      string activityName = null,
      bool includeChecked = false)
        {
            try
            {
                // Get current student's section if not provided
                if (!sectionId.HasValue)
                {
                    var currentStudent = await _context.Students
                        .FirstOrDefaultAsync(s => s.Id == currentStudentId);

                    if (currentStudent != null)
                    {
                        sectionId = currentStudent.SectionId;
                    }
                }

                // Get all students in the section
                var allStudents = await _context.Students
                    .Where(s => s.SectionId == sectionId)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => s.Id)
                    .ToListAsync();

                var eligibleStudentIds = new HashSet<int>(allStudents);

                // If activity name is provided, filter based on it
                if (!string.IsNullOrEmpty(activityName))
                {
                    var templateIds = await _context.ActivityTemplates
                        .Where(at => at.Name == activityName && at.SectionId == sectionId && at.IsActive)
                        .Select(at => at.Id)
                        .ToListAsync();

                    if (templateIds.Any() && !includeChecked)
                    {
                        var studentsWithNonZeroScores = await _context.StudentSubmissions
                            .Where(ss => templateIds.Contains(ss.ActivityTemplateId) && ss.Points > 0)
                            .Select(ss => ss.StudentId)
                            .ToListAsync();

                        eligibleStudentIds.ExceptWith(studentsWithNonZeroScores);
                    }
                }

                // Final query - strictly alphabetical
                var query = _context.Students
                    .Where(s => eligibleStudentIds.Contains(s.Id))
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName);

                // Find current student's position in alphabetical order
                var orderedStudents = await query.Select(s => s.Id).ToListAsync();
                var currentIndex = orderedStudents.IndexOf(currentStudentId);

                int nextIndex = (currentIndex >= 0 && currentIndex + 1 < orderedStudents.Count)
                    ? currentIndex + 1
                    : 0; // wrap around to first

                if (!orderedStudents.Any())
                {
                    return (false, "No students found matching criteria.", null);
                }

                return (true, null, orderedStudents[nextIndex]);
            }
            catch (Exception ex)
            {
                return (false, $"Error finding next student: {ex.Message}", null);
            }
        }

    }
}
