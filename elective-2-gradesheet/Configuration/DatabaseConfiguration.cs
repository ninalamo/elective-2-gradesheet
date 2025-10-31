using Microsoft.EntityFrameworkCore;
using elective_2_gradesheet.Data;
using elective_2_gradesheet.Services;

namespace elective_2_gradesheet.Configuration
{
    public class DatabaseConfiguration
    {
        public const string SqlServerProvider = "SqlServer";
        public const string SqliteProvider = "SQLite";
        
        public static string GetProvider(IConfiguration configuration)
        {
            return configuration.GetValue<string>("DatabaseSettings:Provider") ?? SqlServerProvider;
        }
        
        public static bool IsSqlite(IConfiguration configuration)
        {
            return GetProvider(configuration).Equals(SqliteProvider, StringComparison.OrdinalIgnoreCase);
        }
        
        public static bool IsSqlServer(IConfiguration configuration)
        {
            return GetProvider(configuration).Equals(SqlServerProvider, StringComparison.OrdinalIgnoreCase);
        }
        
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var provider = GetProvider(configuration);
            
            if (IsSqlite(configuration))
            {
                ConfigureSqliteServices(services, configuration);
            }
            else
            {
                ConfigureSqlServerServices(services, configuration);
            }
            
            Console.WriteLine($"Database Provider: {provider}");
        }
        
        private static void ConfigureSqliteServices(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SqliteConnection");
            
            // Ensure the Data directory exists
            var dataDirectory = configuration.GetValue<string>("DatabaseSettings:SqliteDataDirectory") ?? "Data";
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }
            
            services.AddDbContext<ApplicationDbLiteContext>(options =>
                options.UseSqlite(connectionString));
                
            // Register the generic DbContext to point to ApplicationDbLiteContext for ActivityTemplateService
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbLiteContext>());
                
            services.AddScoped<IGradeService, GradeDbLiteService>();
            services.AddScoped<ISectionService, SectionDbLiteService>();
            
            Console.WriteLine($"SQLite Connection: {connectionString}");
        }
        
        private static void ConfigureSqlServerServices(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SqlServerConnection") 
                ?? configuration.GetConnectionString("DefaultConnection"); // Fallback for backwards compatibility
                
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
                
            // Register the generic DbContext to point to ApplicationDbContext for ActivityTemplateService
            services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
                
            services.AddScoped<IGradeService, GradeDbService>();
            services.AddScoped<ISectionService, SectionDbService>();
            
            Console.WriteLine($"SQL Server Connection: {connectionString}");
        }
        
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using var scope = serviceProvider.CreateScope();
            
            try
            {
                if (IsSqlite(configuration))
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbLiteContext>();
                    Console.WriteLine("Ensuring SQLite database is created...");
                    await context.Database.EnsureCreatedAsync();
                    Console.WriteLine("SQLite database setup completed.");
                }
                else
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                    
                    if (pendingMigrations.Any())
                    {
                        Console.WriteLine($"Applying {pendingMigrations.Count()} pending SQL Server migrations...");
                        await context.Database.MigrateAsync();
                        Console.WriteLine("SQL Server database migrations applied successfully.");
                    }
                    else
                    {
                        Console.WriteLine("SQL Server database is up to date.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during database initialization: {ex.Message}");
                throw; // Re-throw to prevent app startup with broken database
            }
        }
    }
}
