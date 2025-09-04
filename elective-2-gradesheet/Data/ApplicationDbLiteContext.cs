using elective_2_gradesheet.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace elective_2_gradesheet.Data
{
    public class ApplicationDbLiteContext : DbContext
    {
        public ApplicationDbLiteContext(DbContextOptions<ApplicationDbLiteContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<ActivityTemplate> ActivityTemplates { get; set; }
        public DbSet<StudentSubmission> StudentSubmissions { get; set; }
        public DbSet<Section> Sections { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SQLite-specific configurations
            
            // Add a unique index on Email to prevent duplicates
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasIndex(s => s.Email).IsUnique();
            });

            modelBuilder.Entity<Section>(entity =>
            {
                entity.HasIndex(s => new { s.Name, s.SchoolYear }).IsUnique();
            });

            // SQLite-specific foreign key configuration
            modelBuilder.Entity<StudentSubmission>(entity =>
            {
                entity.HasOne(ss => ss.Student)
                    .WithMany(s => s.Activities)
                    .HasForeignKey(ss => ss.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                entity.HasOne(ss => ss.ActivityTemplate)
                    .WithMany(at => at.StudentSubmissions)
                    .HasForeignKey(ss => ss.ActivityTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Composite index for performance
                entity.HasIndex(ss => new { ss.StudentId, ss.ActivityTemplateId }).IsUnique();
            });

            modelBuilder.Entity<ActivityTemplate>(entity =>
            {
                entity.HasOne(at => at.Section)
                    .WithMany()
                    .HasForeignKey(at => at.SectionId)
                    .OnDelete(DeleteBehavior.Cascade);
                    
                // Unique composite index to prevent duplicate activity templates
                entity.HasIndex(at => new { at.SectionId, at.Period, at.Name }).IsUnique();
                
                // Additional index for common queries (keeping the original for performance)
                entity.HasIndex(at => new { at.SectionId, at.Period });
            });

            // SQLite doesn't have native enum support, so we configure it as string
            modelBuilder.Entity<ActivityTemplate>()
                .Property(e => e.Period)
                .HasConversion<string>();
        }
    }
}
