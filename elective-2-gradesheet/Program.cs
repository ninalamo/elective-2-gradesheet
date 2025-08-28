using elective_2_gradesheet.Data;
using elective_2_gradesheet.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<ICsvParsingService, CsvParsingService>(); 


var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Ensure database is created
    context.Database.EnsureCreated();
    
    // Seed data if no sections exist
    if (!context.Sections.Any())
    {
        context.Sections.AddRange(
            new elective_2_gradesheet.Data.Entities.Section { Name = "BSIT-31A1", SchoolYear = "2025-2026", IsActive = true },
            new elective_2_gradesheet.Data.Entities.Section { Name = "BSIT-31A2", SchoolYear = "2025-2026", IsActive = true },
            new elective_2_gradesheet.Data.Entities.Section { Name = "BSIT-31A3", SchoolYear = "2025-2026", IsActive = true }
        );
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Records}/{id?}");

app.Run();
