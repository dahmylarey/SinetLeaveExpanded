using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SinetLeaveManagement.Data;
using SinetLeaveManagement.Hubs;
using SinetLeaveManagement.Models;
using SinetLeaveManagement.Services;

var builder = WebApplication.CreateBuilder(args);

//Configure Serilog logging
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

//Configure EF Core & SQL Server (localdb / adjust as needed)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//Configure Identity with custom ApplicationUser & Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

//Register your services properly
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IEmailService, EmailService>();  
builder.Services.AddScoped<IPdfService, PdfService>();

//Add SignalR
builder.Services.AddSignalR();

//Add MVC & runtime compilation
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

//Add Razor Pages if using Identity UI
builder.Services.AddRazorPages();

var app = builder.Build();

//Seed roles/admin user (once on startup)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services);
}

//Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//Map SignalR hub
app.MapHub<NotificationHub>("/notificationHub");

//Map controllers & Identity pages
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Leave}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
