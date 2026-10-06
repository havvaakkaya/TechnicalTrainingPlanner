using Microsoft.EntityFrameworkCore;
using TechnicalTrainingPlanner.Infrastructure.Persistence;
using TechnicalTrainingPlanner.Infrastructure.Seed;
using TechnicalTrainingPlanner.Application.Employees;
using TechnicalTrainingPlanner.Application.Catalog;
using TechnicalTrainingPlanner.Application.Sessions;
using TechnicalTrainingPlanner.Application.Needs;
using TechnicalTrainingPlanner.Application.Planning;
using TechnicalTrainingPlanner.Application.Queries;
using TechnicalTrainingPlanner.Infrastructure.Export;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Veritabanı bağlantı bilgisi bulunamadı.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<EmployeeQueryService>();
builder.Services.AddScoped<EmployeeCommandService>();
builder.Services.AddScoped<TrainingHistoryService>();
builder.Services.AddScoped<TrainingCatalogService>();
builder.Services.AddScoped<TrainingCatalogCommandService>();
builder.Services.AddScoped<SessionQueryService>();
builder.Services.AddScoped<SessionCommandService>();
builder.Services.AddScoped<ScenarioFingerprintService>();
builder.Services.AddScoped<NeedAnalysisService>();
builder.Services.AddScoped<PlanningScenarioService>();
builder.Services.AddScoped<GreedyPlanner>();
builder.Services.AddScoped<CpSatPlanner>();
builder.Services.AddScoped<PlanningService>();
builder.Services.AddScoped<ScreenQueryService>();
builder.Services.AddScoped<ExcelExportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    PilotDataSeeder.SeedAsync(db).GetAwaiter().GetResult();
}
app.Run();
