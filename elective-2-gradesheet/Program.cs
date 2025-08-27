using elective_2_gradesheet.Data;
using elective_2_gradesheet.Services;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        // Create builder for the WebApplication
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        // Configure the database connection (using SQLite)
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Add services for dependency injection
        builder.Services.AddScoped<IGradeService, GradeService>();
        builder.Services.AddScoped<ICsvParsingService, CsvParsingService>();

        // Build the app
        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios.
            app.UseHsts();
        }

        app.UseHttpsRedirection();  // Redirect HTTP requests to HTTPS
        app.UseStaticFiles();       // Enable static file serving (CSS, JS, images)

        app.UseRouting();  // Enable routing for controllers

        app.UseAuthorization();  // Enable authorization middleware (if needed)

        // Set up the default route for your application
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Records}/{id?}");

        // Run the application
        app.Run();
    }
}
