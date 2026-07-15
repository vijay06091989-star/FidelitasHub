using FidelitasHub.Data;
using FidelitasHub.Services.Attendance;
using FidelitasHub.Services.BackgroundServices;
using FidelitasHub.Utilities;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

//==================================================
// Add services to the container.
//==================================================

builder.Services.AddControllersWithViews();

builder.Services.AddHostedService<AttendanceAutoPunchOutService>();

builder.Services.AddScoped<IAttendanceRegisterService, AttendanceRegisterService>();

builder.Services.AddScoped<IAttendanceProcessingService,
    AttendanceProcessingService>();

//==================================================
// SQL Server Database
//==================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

//==================================================
// Master Sequence Generator
//==================================================

builder.Services.AddScoped<MasterSequenceGenerator>();

//==================================================
// Enable Session
//==================================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

//==================================================
// Configure HTTP Request Pipeline
//==================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

//==================================================
// Enable Session
//==================================================

app.UseSession();

app.UseAuthorization();

//==================================================
// Default Route
//==================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();