using elective_2_gradesheet.Data;
using elective_2_gradesheet.Data.Entities;
using elective_2_gradesheet.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace elective_2_gradesheet.Tests.Services
{
    public class GradeServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly IGradeService _service;

        public GradeServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options, null);
            _service = new GradeService(_context);
        }

        [Fact]
        public async Task GetNextStudentAsync_NoStudents_ReturnsNoStudentFound()
        {
            // Arrange
            var currentStudentId = 1;

            // Act
            var result = await _service.GetNextStudentAsync(currentStudentId);

            // Assert
            Assert.False(result.success);
            Assert.Equal("No students found matching criteria.", result.message);
            Assert.Null(result.studentId);
        }

        [Fact]
        public async Task GetNextStudentAsync_OneStudent_ReturnsOnlyStudent()
        {
            // Arrange
            var student = new Student 
            { 
                Id = 1, 
                FirstName = "John", 
                LastName = "Doe", 
                Email = "john@example.com", 
                SectionId = 1,
                IsActive = true 
            };
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetNextStudentAsync(1);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.message);
            Assert.Equal(1, result.studentId);
        }

        [Fact]
        public async Task GetNextStudentAsync_MultipleStudents_ReturnsNextStudent()
        {
            // Arrange
            var section = new Section { Id = 1, Name = "Test Section", IsActive = true };
            _context.Sections.Add(section);

            var students = new List<Student>
            {
                new Student { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", SectionId = 1, IsActive = true },
                new Student { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", SectionId = 1, IsActive = true },
                new Student { Id = 3, FirstName = "Bob", LastName = "Wilson", Email = "bob@example.com", SectionId = 1, IsActive = true }
            };
            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();

            // Act - Get next student after ID 1
            var result = await _service.GetNextStudentAsync(1);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.message);
            Assert.Equal(2, result.studentId);

            // Act - Get next student after last ID (should wrap around)
            result = await _service.GetNextStudentAsync(3);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.message);
            Assert.Equal(1, result.studentId);
        }

        [Fact]
        public async Task GetNextStudentAsync_WithActivityFilter_ReturnsCorrectStudent()
        {
            // Arrange
            var section = new Section { Id = 1, Name = "Test Section", IsActive = true };
            _context.Sections.Add(section);

            var activityTemplate = new ActivityTemplate 
            { 
                Id = 1, 
                Name = "Test Activity",
                SectionId = 1,
                Period = GradingPeriod.Prelim,
                MaxPoints = 100,
                IsActive = true
            };
            _context.ActivityTemplates.Add(activityTemplate);

            var students = new List<Student>
            {
                new Student { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", SectionId = 1, IsActive = true },
                new Student { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", SectionId = 1, IsActive = true },
                new Student { Id = 3, FirstName = "Bob", LastName = "Wilson", Email = "bob@example.com", SectionId = 1, IsActive = true }
            };
            _context.Students.AddRange(students);

            var submissions = new List<StudentSubmission>
            {
                new StudentSubmission { StudentId = 1, ActivityTemplateId = 1, Points = 90 },
                new StudentSubmission { StudentId = 2, ActivityTemplateId = 1, Points = 0 },
                new StudentSubmission { StudentId = 3, ActivityTemplateId = 1, Points = 85 }
            };
            _context.StudentSubmissions.AddRange(submissions);

            await _context.SaveChangesAsync();

            // Act - Get next student with unchecked activity (Points = 0)
            var result = await _service.GetNextStudentAsync(1, activityName: "Test Activity", includeChecked: false);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.message);
            Assert.Equal(2, result.studentId); // Should return student 2 who has Points = 0

            // Act - Get next student including checked activities
            result = await _service.GetNextStudentAsync(1, activityName: "Test Activity", includeChecked: true);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.message);
            Assert.Equal(2, result.studentId); // Should return student 2 (next ID after 1)
        }

        [Fact]
        public async Task GetNextStudentAsync_DifferentSections_OnlyReturnsStudentsInSameSection()
        {
            // Arrange
            var sections = new List<Section>
            {
                new Section { Id = 1, Name = "Section A", IsActive = true },
                new Section { Id = 2, Name = "Section B", IsActive = true }
            };
            _context.Sections.AddRange(sections);

            var students = new List<Student>
            {
                new Student { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", SectionId = 1, IsActive = true },
                new Student { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", SectionId = 2, IsActive = true },
                new Student { Id = 3, FirstName = "Bob", LastName = "Wilson", Email = "bob@example.com", SectionId = 1, IsActive = true }
            };
            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();

            // Act - Get next student in section 1
            var result = await _service.GetNextStudentAsync(1, sectionId: 1);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.message);
            Assert.Equal(3, result.studentId); // Should skip student 2 (different section) and return student 3
        }

        [Fact]
        public async Task GetNextStudentAsync_InactiveStudents_ExcludesInactiveStudents()
        {
            // Arrange
            var section = new Section { Id = 1, Name = "Test Section", IsActive = true };
            _context.Sections.Add(section);

            var students = new List<Student>
            {
                new Student { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com", SectionId = 1, IsActive = true },
                new Student { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", SectionId = 1, IsActive = false },
                new Student { Id = 3, FirstName = "Bob", LastName = "Wilson", Email = "bob@example.com", SectionId = 1, IsActive = true }
            };
            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();

            // Act - Get next student after ID 1
            var result = await _service.GetNextStudentAsync(1);

            // Assert
            Assert.True(result.success);
            Assert.Null(result.message);
            Assert.Equal(3, result.studentId); // Should skip student 2 (inactive) and return student 3
        }
    }
}
