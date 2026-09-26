using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Load .env configuration if present
var currentDir = Directory.GetCurrentDirectory();
var envFiles = new[]
{
    Path.Combine(currentDir, ".env"),
    Path.Combine(currentDir, "..", ".env")
};

foreach (var envPath in envFiles)
{
    if (File.Exists(envPath))
    {
        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
            var parts = trimmed.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var val = parts[1].Trim().Trim('"', '\'');
                Environment.SetEnvironmentVariable(key, val);
                if (key.Equals("MONGODB_URI", StringComparison.OrdinalIgnoreCase) || 
                    key.Equals("MONGODB_CONNECTIONSTRING", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Configuration["MongoDB:ConnectionString"] = val;
                }
                if (key.Equals("MONGODB_DB_NAME", StringComparison.OrdinalIgnoreCase) || 
                    key.Equals("MONGODB_DATABASE", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Configuration["MongoDB:DatabaseName"] = val;
                }
            }
        }
        break;
    }
}

// Configure MongoDB options
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));

// Register Services
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IMemberService, MemberService>();
builder.Services.AddSingleton<IEventService, EventService>();
builder.Services.AddSingleton<IAttendanceService, AttendanceService>();
builder.Services.AddSingleton<IDashboardService, DashboardService>();

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "ACM_AMTICS_AUTH";
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure admin credentials and database collections are seeded
using (var scope = app.Services.CreateScope())
{
    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
    await userService.SeedDefaultAdminAsync();

    // Trigger constructor seeding for Members, Events, and Attendance collections in MongoDB
    scope.ServiceProvider.GetRequiredService<IMemberService>();
    scope.ServiceProvider.GetRequiredService<IEventService>();
    scope.ServiceProvider.GetRequiredService<IAttendanceService>();
}

app.Run();
