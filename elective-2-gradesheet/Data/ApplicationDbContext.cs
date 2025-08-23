using elective_2_gradesheet.Data.Entities;
using Microsoft.EntityFrameworkCore;


namespace elective_2_gradesheet.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Activity> Activities { get; set; }  // Keep old table for migration
        public DbSet<Section> Sections { get; set; }
        
        // New entities
        public DbSet<NewActivity> NewActivities { get; set; }
        public DbSet<StudentActivity> StudentActivities { get; set; }
        public DbSet<Rubric> Rubrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add a unique index on StudentNumber to prevent duplicates
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<Section>()
                .HasIndex(s => new { s.Name, s.SchoolYear })
                .IsUnique();

            // Configure NewActivity entity
            modelBuilder.Entity<NewActivity>(entity =>
            {
                entity.ToTable("Activities_New"); // Different table name to avoid conflict during migration
                
                // Unique index on Name + SectionId + SchoolYear + Period
                entity.HasIndex(e => new { e.Name, e.SectionId, e.SchoolYear, e.Period })
                      .IsUnique()
                      .HasDatabaseName("IX_NewActivity_Unique");

                // Configure relationships
                entity.HasOne(a => a.Section)
                      .WithMany()
                      .HasForeignKey(a => a.SectionId)
                      .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete activities when section is deleted

                // Configure JSON column for better performance if using SQL Server 2022+
                // entity.Property(e => e.RubricJson).HasColumnType("nvarchar(max)");
            });

            // Configure StudentActivity entity
            modelBuilder.Entity<StudentActivity>(entity =>
            {
                // Unique index on StudentId + ActivityId to prevent duplicate submissions
                entity.HasIndex(e => new { e.StudentId, e.ActivityId })
                      .IsUnique()
                      .HasDatabaseName("IX_StudentActivity_Unique");

                // Configure relationships
                entity.HasOne(sa => sa.Student)
                      .WithMany()
                      .HasForeignKey(sa => sa.StudentId)
                      .OnDelete(DeleteBehavior.Cascade); // When student is deleted, delete their activities

                entity.HasOne(sa => sa.Activity)
                      .WithMany(a => a.StudentActivities)
                      .HasForeignKey(sa => sa.ActivityId)
                      .OnDelete(DeleteBehavior.Cascade); // When activity is deleted, delete student submissions
            });

            // Configure Rubric entity
            modelBuilder.Entity<Rubric>(entity =>
            {
                // Index on ActivityId for better query performance
                entity.HasIndex(r => r.ActivityId)
                      .HasDatabaseName("IX_Rubric_ActivityId");

                // Configure relationship
                entity.HasOne(r => r.Activity)
                      .WithMany(a => a.Rubrics)
                      .HasForeignKey(r => r.ActivityId)
                      .OnDelete(DeleteBehavior.Cascade); // When activity is deleted, delete its rubrics
            });
        }
    }
}
