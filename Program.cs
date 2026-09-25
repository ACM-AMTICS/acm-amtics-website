using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

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
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

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
