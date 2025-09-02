using elective_2_gradesheet.Configuration;
using elective_2_gradesheet.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure database provider based on settings
DatabaseConfiguration.ConfigureServices(builder.Services, builder.Configuration);
builder.Services.AddScoped<ICsvParsingService, CsvParsingService>(); 
builder.Services.AddScoped<IGitService, GitService>();
builder.Services.AddScoped<IActivityTemplateService, ActivityTemplateService>();


var app = builder.Build();

// Initialize database
await DatabaseConfiguration.InitializeDatabaseAsync(app.Services, app.Configuration);

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
