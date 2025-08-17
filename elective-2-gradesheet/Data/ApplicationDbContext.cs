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
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Section> Sections { get; set; }

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
        }
    }
}
