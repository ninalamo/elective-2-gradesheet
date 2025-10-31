using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace elective_2_gradesheet.Data
{
    /// <summary>
    /// Design-time factory for SQL Server migrations
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Use a default SQL Server connection string for design-time operations
            optionsBuilder.UseSqlServer("Server=127.0.0.1,1433;Database=elective_2;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }

    /// <summary>
    /// Design-time factory for SQLite (not typically used with migrations, but included for completeness)
    /// </summary>
    public class ApplicationDbLiteContextFactory : IDesignTimeDbContextFactory<ApplicationDbLiteContext>
    {
        public ApplicationDbLiteContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbLiteContext>();
            optionsBuilder.UseSqlite("Data Source=Data/elective_gradesheet_dev.db");

            return new ApplicationDbLiteContext(optionsBuilder.Options);
        }
    }
}
