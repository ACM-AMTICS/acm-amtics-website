using acm_amtics_website.Models;
using acm_amtics_website.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Load .env configuration if present (primarily for local development)
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

// Allow container/system environment variables to override or provide MongoDB settings
var directMongoUri = Environment.GetEnvironmentVariable("MONGODB_URI") ?? 
                     Environment.GetEnvironmentVariable("MONGODB_CONNECTIONSTRING");
if (!string.IsNullOrWhiteSpace(directMongoUri))
{
    builder.Configuration["MongoDB:ConnectionString"] = directMongoUri;
}

var directDbName = Environment.GetEnvironmentVariable("MONGODB_DB_NAME") ?? 
                   Environment.GetEnvironmentVariable("MONGODB_DATABASE");
if (!string.IsNullOrWhiteSpace(directDbName))
{
    builder.Configuration["MongoDB:DatabaseName"] = directDbName;
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
builder.Services.AddSingleton<IProfileService, ProfileService>();

// Configure Antiforgery with header support for AJAX calls
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "ACM_AMTICS_XSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "ACM_AMTICS_AUTH";
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;

        // Return 401/403 for API requests instead of redirecting to login HTML
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// Health Checks
builder.Services.AddHealthChecks();

// Add MVC services
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Forwarded headers for reverse proxies (Nginx, Traefik, Docker, Cloud Run, Azure)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint for container orchestrators and load balancers
app.MapHealthChecks("/health");

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
