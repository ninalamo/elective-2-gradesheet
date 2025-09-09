using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Helpers;
using elective_2_gradesheet.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Text.Json;
using elective_2_gradesheet.Controllers;
using System.Collections.Concurrent;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace elective_2_gradesheet.Services
{
    public class GradeDbLiteService : IGradeService
    {
        private readonly ApplicationDbLiteContext _context;
        private readonly RepositoryService _repositoryService;
        private readonly RubricScoringService _rubricScoringService;
        private readonly ConcurrentDictionary<string, BulkGradingSession> _sessions = new();
        private readonly ConcurrentDictionary<string, BulkGradingProgressUpdate> _progressUpdates = new();

        // The database context is injected via the constructor.
        public GradeDbLiteService(ApplicationDbLiteContext context, RepositoryService repositoryService, RubricScoringService rubricScoringService)
        {
            _context = context;
            _repositoryService = repositoryService;
            _rubricScoringService = rubricScoringService;
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

        public async Task UpdateActivityAsync(int studentId, double points, double maxPoints, GradingPeriod period, string? tag, string? otherTag, string? githubLink, string? status, int? activityId = default, string? activityName = "", int? newId = 0)
        {
            // Input validation
            if (studentId <= 0) return;
            if (maxPoints <= 0) return;
            
            // Normalize null/empty parameters to proper defaults
            tag = string.IsNullOrWhiteSpace(tag) ? "Other" : tag;
            otherTag = string.IsNullOrWhiteSpace(otherTag) ? string.Empty : otherTag;
            githubLink = string.IsNullOrWhiteSpace(githubLink) ? null : githubLink;
            status = string.IsNullOrWhiteSpace(status) ? "Missing" : status;
            activityName = string.IsNullOrWhiteSpace(activityName) ? string.Empty : activityName;

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            StudentSubmission? existingSubmission = null;
            ActivityTemplate? activityTemplate = null;

            if (activityId != null && activityId > 0)
            {
                // Case: Update an existing student submission by ID
                existingSubmission = await _context.StudentSubmissions
                    .Include(ss => ss.ActivityTemplate)
                    .FirstOrDefaultAsync(ss => ss.Id == activityId);
                    
                if (existingSubmission != null)
                {
                    activityTemplate = existingSubmission.ActivityTemplate;
                }
            }
            else
            {
                // Case: activityId is 0 or null - need to find by activityName and period
                // This handles the "Fix" button case where activityId=0 but record might exist
                
                if (string.IsNullOrWhiteSpace(activityName))
                {
                    return; // Cannot process without activity name
                }

                // First, try to find the activity template (case-insensitive, trimmed comparison)
                var normalizedActivityName = activityName.Trim();
                Console.WriteLine($"[DEBUG SQLite] Searching for ActivityTemplate: '{normalizedActivityName}', Period: {period}, SectionId: {student.SectionId}");
                
                activityTemplate = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Name.Trim().ToLower() == normalizedActivityName.ToLower() &&
                                             at.Period == period &&
                                             at.SectionId == student.SectionId);

                if (activityTemplate == null)
                {
                    // Create a new activity template
                    Console.WriteLine($"[DEBUG SQLite] No existing ActivityTemplate found. Creating new one for '{activityName}'");
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
                    Console.WriteLine($"[DEBUG SQLite] Created new ActivityTemplate with ID: {activityTemplate.Id}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG SQLite] Found existing ActivityTemplate with ID: {activityTemplate.Id}, Name: '{activityTemplate.Name}'");
                }

                // Now check if a StudentSubmission already exists for this student and template
                existingSubmission = await _context.StudentSubmissions
                    .FirstOrDefaultAsync(ss => ss.StudentId == studentId && 
                                             ss.ActivityTemplateId == activityTemplate.Id);
            }

            // Update or create the submission
            if (existingSubmission != null)
            {
                // Update existing submission
                existingSubmission.Points = points;
                existingSubmission.Status = status;
                existingSubmission.GithubLink = githubLink;
                existingSubmission.UpdatedDate = DateTime.UtcNow;

                // Set submission date when status changes to submitted
                if ((status == "Submitted" || status == "Turned In") && existingSubmission.SubmissionDate == null)
                {
                    existingSubmission.SubmissionDate = DateTime.UtcNow;
                }
                // Clear submission date if status is no longer submitted
                else if (status != "Submitted" && status != "Turned In" && existingSubmission.SubmissionDate != null)
                {
                    existingSubmission.SubmissionDate = null;
                }
            }
            else if (activityTemplate != null)
            {
                // Create new submission
                var newSubmission = new StudentSubmission
                {
                    StudentId = studentId,
                    ActivityTemplateId = activityTemplate.Id,
                    Points = points,
                    Status = status,
                    GithubLink = githubLink,
                    SubmissionDate = (status == "Submitted" || status == "Turned In") ? DateTime.UtcNow : null,
                    GradedDate = null
                };
                _context.StudentSubmissions.Add(newSubmission);
            }
            else
            {
                return; // Should not reach here, but just in case
            }

            // Update the activity template's max points and tag if different
            if (activityTemplate != null)
            {
                bool templateChanged = false;
                
                if (activityTemplate.MaxPoints != maxPoints)
                {
                    activityTemplate.MaxPoints = maxPoints;
                    templateChanged = true;
                }

                // Update the tag if it's different (tag is stored in ActivityTemplate)
                var newTag = tag == "Other" ? otherTag : tag;
                if (!string.IsNullOrWhiteSpace(newTag) && activityTemplate.Tag != newTag)
                {
                    activityTemplate.Tag = newTag;
                    templateChanged = true;
                }
                
                if (templateChanged)
                {
                    activityTemplate.UpdatedDate = DateTime.UtcNow;
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

            // SQLite optimization: Use a single transaction for all operations
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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

                    // Save changes for this activity group
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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

        // Bulk grading methods implementation
        public async Task<List<Student>> GetStudentsBySectionAsync(int sectionId)
        {
            return await _context.Students
                .Where(s => s.SectionId == sectionId)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }

        public async Task<List<ActivityTemplate>> GetActivityTemplatesBySectionAsync(int sectionId)
        {
            return await _context.ActivityTemplates
                .Where(at => at.SectionId == sectionId && at.IsActive)
                .OrderBy(at => at.Period)
                .ThenBy(at => at.Name)
                .ToListAsync();
        }

        public async Task<(bool success, string message, List<BulkGradingResult> results)> ProcessBulkGradingAsync(BulkGradingRequest request)
        {
            var results = new List<BulkGradingResult>();
            
            try
            {
                // Get the activity template with rubric
                var activityTemplate = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Id == request.ActivityTemplateId && at.IsActive);
                    
                if (activityTemplate == null)
                {
                    return (false, "Activity template not found.", results);
                }

                // Parse the rubric if available
                List<RubricItem>? rubric = null;
                if (!string.IsNullOrEmpty(activityTemplate.RubricJson))
                {
                    try
                    {
                        rubric = JsonSerializer.Deserialize<List<RubricItem>>(activityTemplate.RubricJson, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (JsonException)
                    {
                        // Continue without rubric if it's invalid
                        rubric = null;
                    }
                }

                // Process each student submission
                foreach (var submission in request.Submissions)
                {
                    var result = new BulkGradingResult
                    {
                        StudentId = submission.StudentId
                    };

                    try
                    {
                        // Get student info
                        var student = await _context.Students.FindAsync(submission.StudentId);
                        if (student == null)
                        {
                            result.Success = false;
                            result.Message = "Student not found";
                            results.Add(result);
                            continue;
                        }
                        result.StudentName = student.GetFullName();

                        // Check for existing submission
                        var existingSubmission = await _context.StudentSubmissions
                            .FirstOrDefaultAsync(ss => ss.StudentId == submission.StudentId && 
                                               ss.ActivityTemplateId == request.ActivityTemplateId);

                        // Skip if already graded or has non-zero grades (as per requirements)
                        if (existingSubmission != null && 
                            (existingSubmission.Status.Equals("Graded", StringComparison.OrdinalIgnoreCase) || existingSubmission.Points > 0))
                        {
                            result.Success = false;
                            result.Message = existingSubmission.Status.Equals("Graded", StringComparison.OrdinalIgnoreCase) 
                                ? "Student already graded. Skipped."
                                : "Student already has non-zero grade. Skipped.";
                            result.Points = existingSubmission.Points;
                            result.Status = existingSubmission.Status;
                            results.Add(result);
                            continue;
                        }

                        // Determine status and points based on GitHub link
                        string status;
                        double points = 0;
                        var scoringDetails = new List<string>();
                        
                        if (string.IsNullOrWhiteSpace(submission.GitHubLink))
                        {
                            status = "Not Turned In";
                            result.Message = "No GitHub repository provided";
                        }
                        else
                        {
                            status = "Turned In";
                            result.Message = "GitHub repository submitted";
                            
                            // If rubric is available, we could potentially score it here
                            // For now, we'll just mark it as submitted with 0 points
                            // The actual scoring would happen when the repository is cloned and analyzed
                            if (rubric != null)
                            {
                                // Note: For full implementation, you'd want to clone the repo and score it
                                // For now, we'll just award partial points for having a GitHub link
                                points = Math.Min(activityTemplate.MaxPoints * 0.1, 5); // 10% or 5 points, whichever is smaller
                                result.Message += " (Partial credit for submission - requires manual scoring)";
                                scoringDetails.Add($"GitHub link provided: {submission.GitHubLink}");
                                scoringDetails.Add("Repository content analysis pending");
                            }
                        }

                        // Create or update the student submission
                        if (existingSubmission != null)
                        {
                            // Update existing submission
                            existingSubmission.Status = status;
                            existingSubmission.GithubLink = submission.GitHubLink;
                            existingSubmission.Points = points;
                            existingSubmission.UpdatedDate = DateTime.UtcNow;
                            
                            if (status == "Turned In" && existingSubmission.SubmissionDate == null)
                            {
                                existingSubmission.SubmissionDate = DateTime.UtcNow;
                            }
                        }
                        else
                        {
                            // Create new submission
                            var newSubmission = new StudentSubmission
                            {
                                StudentId = submission.StudentId,
                                ActivityTemplateId = request.ActivityTemplateId,
                                Points = points,
                                Status = status,
                                GithubLink = submission.GitHubLink,
                                SubmissionDate = status == "Turned In" ? DateTime.UtcNow : null,
                                GradedDate = null
                            };
                            _context.StudentSubmissions.Add(newSubmission);
                        }

                        result.Success = true;
                        result.Points = points;
                        result.Status = status;
                        result.ScoringDetails = scoringDetails;
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.Message = $"Error processing student: {ex.Message}";
                    }
                    
                    results.Add(result);
                }

                // Save all changes
                await _context.SaveChangesAsync();
                
                var successCount = results.Count(r => r.Success);
                var message = $"Processed {successCount} out of {results.Count} students successfully.";
                
                return (true, message, results);
            }
            catch (Exception ex)
            {
                return (false, $"Error during bulk processing: {ex.Message}", results);
            }
        }

        // Enhanced bulk grading methods
        public async Task<BulkGradingViewModel> InitializeBulkGradingAsync(int activityTemplateId, int sectionId, List<string>? statusFilters = null, bool includeGraded = false, string? searchTerm = null)
        {
            var activityTemplate = await _context.ActivityTemplates
                .FirstOrDefaultAsync(at => at.Id == activityTemplateId);

            if (activityTemplate == null)
                throw new ArgumentException($"Activity template with ID {activityTemplateId} not found");

            var studentsQuery = _context.Students
                .Where(s => s.SectionId == sectionId);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchTermLower = searchTerm.ToLower();
                studentsQuery = studentsQuery.Where(s => 
                    s.FirstName.ToLower().Contains(searchTermLower) ||
                    s.LastName.ToLower().Contains(searchTermLower) ||
                    s.Email.ToLower().Contains(searchTermLower) ||
                    (s.FirstName + " " + s.LastName).ToLower().Contains(searchTermLower) ||
                    (s.LastName + ", " + s.FirstName).ToLower().Contains(searchTermLower));
            }

            var students = await studentsQuery
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            var existingSubmissions = await _context.StudentSubmissions
                .Where(ss => ss.ActivityTemplateId == activityTemplateId && 
                            students.Select(s => s.Id).Contains(ss.StudentId))
                .ToListAsync();

            var studentViewModels = students.Select(student =>
            {
                var existingSubmission = existingSubmissions.FirstOrDefault(es => es.StudentId == student.Id);
                var hasNonZeroGrade = existingSubmission?.Points > 0;
                var hasTurnedIn = existingSubmission?.Status == "Turned In" || existingSubmission?.Status == "Turned In Late";
                var currentStatus = existingSubmission?.Status ?? "Missing";
                var isGraded = currentStatus.Equals("Graded", StringComparison.OrdinalIgnoreCase);
                
                // Apply status filtering - show all students if no filters, otherwise check if status matches any filter
                var isVisible = true;
                if (statusFilters?.Any() == true || !includeGraded)
                {
                    // Check if current status matches any of the selected status filters
                    var statusMatches = statusFilters?.Any() != true || 
                                       statusFilters.Contains(currentStatus, StringComparer.OrdinalIgnoreCase);
                    
                    // Check if we should include graded entries
                    var gradedMatches = includeGraded || !isGraded;
                    
                    isVisible = statusMatches && gradedMatches;
                }

                return new BulkGradingStudentViewModel
                {
                    StudentId = student.Id,
                    StudentName = $"{student.LastName}, {student.FirstName}",
                    StudentNumber = student.GetStudentNumber(),
                    RepositoryUrl = existingSubmission?.GithubLink,
                    CurrentPoints = existingSubmission?.Points,
                    CurrentStatus = currentStatus,
                    HasExistingSubmission = existingSubmission != null,
                    SubmissionId = existingSubmission?.Id,
                    HasNonZeroGrade = hasNonZeroGrade,
                    HasTurnedIn = hasTurnedIn,
                    IsSelected = !(isGraded || (hasNonZeroGrade && hasTurnedIn)),
                    IsVisible = isVisible
                };
            }).ToList();

            return new BulkGradingViewModel
            {
                ActivityTemplateId = activityTemplateId,
                ActivityTemplateName = activityTemplate.Name,
                SectionId = sectionId,
                Students = studentViewModels,
                StatusFilters = statusFilters ?? new List<string>(),
                IncludeGraded = includeGraded
            };
        }

        public async Task<string> StartBulkProcessingAsync(int activityTemplateId, int sectionId, List<int> selectedStudentIds, bool showNonZeroGrades)
        {
            var sessionId = Guid.NewGuid().ToString();
            
            var session = new BulkGradingSession
            {
                SessionId = sessionId,
                ActivityTemplateId = activityTemplateId,
                SectionId = sectionId,
                ShowNonZeroGrades = showNonZeroGrades,
                Status = BulkGradingStatus.Processing
            };

            _sessions[sessionId] = session;

            // Start background processing
            _ = Task.Run(() => ProcessStudentsAsync(sessionId, selectedStudentIds));

            return sessionId;
        }

        private async Task ProcessStudentsAsync(string sessionId, List<int> selectedStudentIds)
        {
            var session = _sessions[sessionId];
            var progressUpdate = new BulkGradingProgressUpdate
            {
                TotalCount = selectedStudentIds.Count,
                ProcessedCount = 0
            };

            try
            {
                var activityTemplate = await _context.ActivityTemplates
                    .FirstOrDefaultAsync(at => at.Id == session.ActivityTemplateId);

                if (activityTemplate?.RubricJson == null)
                {
                    throw new InvalidOperationException("Activity template must have a rubric defined");
                }

                var rubric = JsonSerializer.Deserialize<List<RubricItem>>(activityTemplate.RubricJson);
                var students = await _context.Students
                    .Where(s => selectedStudentIds.Contains(s.Id))
                    .ToListAsync();

                var existingSubmissions = await _context.StudentSubmissions
                    .Where(ss => ss.ActivityTemplateId == session.ActivityTemplateId && 
                                selectedStudentIds.Contains(ss.StudentId))
                    .ToListAsync();

                var previewItems = new List<BulkGradingPreviewItem>();

                for (int i = 0; i < students.Count; i++)
                {
                    var student = students[i];
                    var existingSubmission = existingSubmissions.FirstOrDefault(es => es.StudentId == student.Id);
                    progressUpdate.StudentId = student.Id;
                    progressUpdate.StudentName = $"{student.LastName}, {student.FirstName}";
                    progressUpdate.RepositoryUrl = existingSubmission?.GithubLink;
                    progressUpdate.CurrentStep = "Starting processing";
                    progressUpdate.ProcessedCount = i;
                    _progressUpdates[sessionId] = progressUpdate;

                    try
                    {
                        var previewItem = await ProcessSingleStudentAsync(student, activityTemplate, rubric!, existingSubmissions, progressUpdate, sessionId);
                        previewItems.Add(previewItem);
                    }
                    catch (Exception ex)
                    {
                        var errorItem = new BulkGradingPreviewItem
                        {
                            StudentId = student.Id,
                            StudentName = $"{student.LastName}, {student.FirstName}",
                            RepositoryUrl = existingSubmission?.GithubLink,
                            NewPoints = 0,
                            NewStatus = "Error",
                            ScoringDetails = new List<string> { $"Error: {ex.Message}" },
                            RequiresApproval = true,
                            IsApproved = false
                        };
                        previewItems.Add(errorItem);
                    }
                }

                progressUpdate.ProcessedCount = students.Count;
                progressUpdate.IsComplete = true;
                progressUpdate.CurrentStep = "Processing complete";
                _progressUpdates[sessionId] = progressUpdate;

                session.PreviewItems = previewItems;
                session.Status = BulkGradingStatus.WaitingForApproval;
            }
            catch (Exception ex)
            {
                progressUpdate.HasError = true;
                progressUpdate.ErrorMessage = ex.Message;
                progressUpdate.IsComplete = true;
                _progressUpdates[sessionId] = progressUpdate;
                
                session.Status = BulkGradingStatus.Failed;
            }
        }

        private async Task<BulkGradingPreviewItem> ProcessSingleStudentAsync(
            Student student, 
            ActivityTemplate activityTemplate,
            List<RubricItem> rubric,
            List<StudentSubmission> existingSubmissions,
            BulkGradingProgressUpdate progressUpdate,
            string sessionId)
        {
            var existingSubmission = existingSubmissions.FirstOrDefault(es => es.StudentId == student.Id);
            var repositoryUrl = existingSubmission?.GithubLink;
            
            if (string.IsNullOrEmpty(repositoryUrl))
            {
                throw new InvalidOperationException($"Student {student.LastName}, {student.FirstName} has no repository URL");
            }
            
            progressUpdate.CurrentStep = "Cloning repository";
            _progressUpdates[sessionId] = progressUpdate;

            var cloneResult = await _repositoryService.CloneRepositoryAsync(repositoryUrl, student.Id);
            if (!cloneResult.Success)
            {
                throw new InvalidOperationException($"Failed to clone repository: {cloneResult.ErrorMessage}");
            }

            try
            {
                progressUpdate.CurrentStep = "Scanning repository";
                _progressUpdates[sessionId] = progressUpdate;

                var scanResult = await _repositoryService.ScanRepositoryAsync(cloneResult.ClonedDirectory!);
                if (!scanResult.Success)
                {
                    throw new InvalidOperationException($"Failed to scan repository: {scanResult.ErrorMessage}");
                }

                progressUpdate.CurrentStep = "Scoring against rubric";
                _progressUpdates[sessionId] = progressUpdate;

                var scoringResult = await _rubricScoringService.ScoreSubmissionAsync(rubric, scanResult.ScannedFiles!);
                
                var previewItem = new BulkGradingPreviewItem
                {
                    StudentId = student.Id,
                    StudentName = $"{student.LastName}, {student.FirstName}",
                    RepositoryUrl = repositoryUrl,
                    NewPoints = scoringResult.TotalPoints,
                    NewStatus = scoringResult.TotalPoints > 0 ? "Completed" : "Not Started",
                    CurrentPoints = existingSubmission?.Points,
                    CurrentStatus = existingSubmission?.Status,
                    IsNew = existingSubmission == null,
                    IsUpdate = existingSubmission != null,
                    ScoringDetails = scoringResult.ScoringDetails,
                    RequiresApproval = existingSubmission != null,
                    IsApproved = existingSubmission == null
                };

                return previewItem;
            }
            finally
            {
                if (!string.IsNullOrEmpty(cloneResult.ClonedDirectory))
                {
                    try
                    {
                        Directory.Delete(cloneResult.ClonedDirectory, true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        public BulkGradingProgressUpdate? GetBulkProcessingProgress(string sessionId)
        {
            return _progressUpdates.TryGetValue(sessionId, out var progress) ? progress : null;
        }

        public BulkGradingSession? GetBulkProcessingSession(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        public void UpdateBulkApprovalStatus(string sessionId, int studentId, bool isApproved)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                var item = session.PreviewItems.FirstOrDefault(pi => pi.StudentId == studentId);
                if (item != null)
                {
                    item.IsApproved = isApproved;
                }
            }
        }

        public void BulkApproveAll(string sessionId, bool isApproved)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                foreach (var item in session.PreviewItems.Where(pi => pi.RequiresApproval))
                {
                    item.IsApproved = isApproved;
                }
            }
        }

        public async Task<bool> SaveBulkGradingResultsAsync(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            session.Status = BulkGradingStatus.Saving;

            try
            {
                var approvedItems = session.PreviewItems.Where(pi => pi.IsApproved).ToList();
                
                foreach (var item in approvedItems)
                {
                    if (item.IsNew)
                    {
                        var submission = new StudentSubmission
                        {
                            StudentId = item.StudentId,
                            ActivityTemplateId = session.ActivityTemplateId,
                            Points = item.NewPoints,
                            Status = item.NewStatus,
                            RubricScoreJson = JsonSerializer.Serialize(item.ScoringDetails),
                            GradedDate = DateTime.UtcNow
                        };
                        _context.StudentSubmissions.Add(submission);
                    }
                    else if (item.IsUpdate)
                    {
                        var existingSubmission = await _context.StudentSubmissions
                            .FirstOrDefaultAsync(ss => ss.StudentId == item.StudentId && 
                                                     ss.ActivityTemplateId == session.ActivityTemplateId);
                        
                        if (existingSubmission != null)
                        {
                            existingSubmission.Points = item.NewPoints;
                            existingSubmission.Status = item.NewStatus;
                            existingSubmission.RubricScoreJson = JsonSerializer.Serialize(item.ScoringDetails);
                            existingSubmission.GradedDate = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                session.Status = BulkGradingStatus.Completed;
                
                return true;
            }
            catch
            {
                session.Status = BulkGradingStatus.Failed;
                return false;
            }
        }

        public void CleanupBulkGradingSession(string sessionId)
        {
            _sessions.TryRemove(sessionId, out _);
            _progressUpdates.TryRemove(sessionId, out _);
        }
        
        public async Task SaveRepositoryUrlsAsync(int activityTemplateId, List<BulkProcessingStudent> students)
        {
            foreach (var student in students)
            {
                // Find or create student submission for this activity
                var existingSubmission = await _context.StudentSubmissions
                    .FirstOrDefaultAsync(ss => ss.StudentId == student.StudentId && 
                                             ss.ActivityTemplateId == activityTemplateId);
                
                if (existingSubmission != null)
                {
                    // Update existing submission with repository URL
                    existingSubmission.GithubLink = student.RepositoryUrl;
                    existingSubmission.UpdatedDate = DateTime.UtcNow;
                }
                else
                {
                    // Create new submission with repository URL
                    var newSubmission = new StudentSubmission
                    {
                        StudentId = student.StudentId,
                        ActivityTemplateId = activityTemplateId,
                        GithubLink = student.RepositoryUrl,
                        Status = "Not Started",
                        Points = 0,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.StudentSubmissions.Add(newSubmission);
                }
            }
            
            await _context.SaveChangesAsync();
        }

        public async Task<GradeSummaryViewModel> GetGradeSummaryAsync(GradingPeriod? period = null, int? sectionId = null)
        {
            var sectionsQuery = _context.Sections
                .Include(s => s.Students)
                .Include(s => s.ActivityTemplates)
                .Where(s => s.IsActive && (!sectionId.HasValue || s.Id == sectionId.Value));

            var sections = await sectionsQuery.ToListAsync();
            var sectionSummaries = new List<SectionSummary>();

            foreach (var section in sections)
            {
                // Filter activity templates by period if specified
                var activityTemplates = section.ActivityTemplates
                    .Where(at => at.IsActive && (!period.HasValue || at.Period == period.Value))
                    .ToList();

                if (!activityTemplates.Any())
                    continue;

                // Get student IDs for this section
                var studentIds = section.Students.Select(s => s.Id).ToList();

                // Get all submissions for students in this section
                var studentSubmissions = await _context.StudentSubmissions
                    .Include(ss => ss.ActivityTemplate)
                    .Where(ss => studentIds.Contains(ss.StudentId) &&
                                (!period.HasValue || ss.ActivityTemplate.Period == period.Value))
                    .ToListAsync();

                var studentSummaries = new List<StudentGradeSummary>();
                var activityNames = activityTemplates
                    .OrderBy(at => at.Period)
                    .ThenBy(at => at.Name)
                    .Select(at => at.Name)
                    .ToList();

                foreach (var student in section.Students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName))
                {
                    var studentActivities = new List<ActivityGradeSummary>();
                    var totalPoints = 0.0;
                    var totalMaxPoints = 0.0;

                    foreach (var template in activityTemplates)
                    {
                        var submission = studentSubmissions
                            .FirstOrDefault(ss => ss.StudentId == student.Id && ss.ActivityTemplateId == template.Id);

                        var points = submission?.Points ?? 0;
                        var maxPoints = template.MaxPoints;
                        var percentage = maxPoints > 0 ? (points / maxPoints) * 100 : 0;

                        studentActivities.Add(new ActivityGradeSummary
                        {
                            ActivityId = submission?.Id ?? 0,
                            ActivityName = template.Name,
                            Points = points,
                            MaxPoints = maxPoints,
                            Percentage = percentage,
                            Status = submission?.Status ?? "Missing",
                            Period = template.Period,
                            Tag = template.Tag ?? "N/A"
                        });

                        totalPoints += points;
                        totalMaxPoints += maxPoints;
                    }

                    var overallPercentage = totalMaxPoints > 0 ? (totalPoints / totalMaxPoints) * 100 : 0;
                    var overallGrade = GetLetterGrade(overallPercentage);

                    studentSummaries.Add(new StudentGradeSummary
                    {
                        StudentId = student.Id,
                        StudentNumber = student.GetStudentNumber(),
                        StudentFullName = student.GetFullName(),
                        SectionName = section.Name,
                        Activities = studentActivities,
                        TotalPoints = totalPoints,
                        TotalMaxPoints = totalMaxPoints,
                        OverallPercentage = overallPercentage,
                        OverallGrade = overallGrade
                    });
                }

                var sectionAverage = studentSummaries.Any() ? studentSummaries.Average(s => s.OverallPercentage) : 0;

                sectionSummaries.Add(new SectionSummary
                {
                    SectionId = section.Id,
                    SectionName = section.Name,
                    SchoolYear = section.SchoolYear,
                    Students = studentSummaries,
                    ActivityNames = activityNames,
                    SectionAverage = sectionAverage
                });
            }

            return new GradeSummaryViewModel
            {
                Sections = sectionSummaries,
                TotalStudents = sectionSummaries.Sum(s => s.Students.Count),
                TotalActivities = sectionSummaries.FirstOrDefault()?.ActivityNames.Count ?? 0,
                FilterPeriod = period,
                FilterSectionId = sectionId
            };
        }

        public async Task<byte[]> ExportGradesToExcelAsync(GradingPeriod? period = null, int? sectionId = null)
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var summaryData = await GetGradeSummaryAsync(period, sectionId);

            using var package = new ExcelPackage();

            foreach (var section in summaryData.Sections)
            {
                var worksheet = package.Workbook.Worksheets.Add($"{section.SectionName}");

                // Add headers
                var col = 1;
                worksheet.Cells[1, col++].Value = "Student Number";
                worksheet.Cells[1, col++].Value = "Student Name";

                // Add activity columns
                var activityStartCol = col;
                foreach (var activityName in section.ActivityNames)
                {
                    worksheet.Cells[1, col].Value = activityName + " (Points)";
                    worksheet.Cells[2, col].Value = "Max Points";
                    col++;
                    worksheet.Cells[1, col].Value = activityName + " (%)";
                    worksheet.Cells[2, col].Value = "Percentage";
                    col++;
                }

                worksheet.Cells[1, col++].Value = "Total Points";
                worksheet.Cells[1, col++].Value = "Total Max Points";
                worksheet.Cells[1, col++].Value = "Overall %";
                worksheet.Cells[1, col++].Value = "Grade";

                // Style the header row
                var headerRange = worksheet.Cells[1, 1, 1, col - 1];
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                headerRange.Style.Border.BorderAround(ExcelBorderStyle.Thick);

                // Add max points row for activities
                col = activityStartCol;
                foreach (var activityName in section.ActivityNames)
                {
                    var maxPoints = section.Students.FirstOrDefault()?
                        .Activities.FirstOrDefault(a => a.ActivityName == activityName)?.MaxPoints ?? 0;
                    worksheet.Cells[2, col].Value = maxPoints;
                    col += 2; // Skip percentage column
                }

                // Add student data
                var row = 3;
                foreach (var student in section.Students)
                {
                    col = 1;
                    worksheet.Cells[row, col++].Value = student.StudentNumber;
                    worksheet.Cells[row, col++].Value = student.StudentFullName;

                    // Add activity scores
                    foreach (var activityName in section.ActivityNames)
                    {
                        var activity = student.Activities.FirstOrDefault(a => a.ActivityName == activityName);
                        worksheet.Cells[row, col++].Value = activity?.Points ?? 0;
                        worksheet.Cells[row, col++].Value = Math.Round(activity?.Percentage ?? 0, 2);
                    }

                    worksheet.Cells[row, col++].Value = student.TotalPoints;
                    worksheet.Cells[row, col++].Value = student.TotalMaxPoints;
                    worksheet.Cells[row, col++].Value = Math.Round(student.OverallPercentage, 2);
                    worksheet.Cells[row, col++].Value = student.OverallGrade;
                    row++;
                }

                // Add section average row
                worksheet.Cells[row + 1, 1].Value = "Section Average";
                worksheet.Cells[row + 1, col - 2].Value = Math.Round(section.SectionAverage, 2);
                var avgRange = worksheet.Cells[row + 1, 1, row + 1, col - 1];
                avgRange.Style.Font.Bold = true;
                avgRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                avgRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Add borders to all data
                var dataRange = worksheet.Cells[1, 1, row + 1, col - 1];
                dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            return await package.GetAsByteArrayAsync();
        }

        private string GetLetterGrade(double percentage)
        {
            return percentage switch
            {
                >= 97 => "A+",
                >= 93 => "A",
                >= 90 => "A-",
                >= 87 => "B+",
                >= 83 => "B",
                >= 80 => "B-",
                >= 77 => "C+",
                >= 73 => "C",
                >= 70 => "C-",
                >= 67 => "D+",
                >= 65 => "D",
                _ => "F"
            };
        }
    }
}
