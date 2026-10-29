// Provides EF Core's UseSqlServer extension method.
using Microsoft.EntityFrameworkCore;
// A successful sign-in creates an authentication cookie for the browser.
using Microsoft.AspNetCore.Authentication.Cookies;
// Makes AppDbContext and DbSeeder available to Program.cs.
using SportHub.Data;
using SportHub.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<FacilityImageService>();
// Handles validation and file storage for Admin-managed Home banners.
builder.Services.AddScoped<HeroSlideImageService>();
builder.Services.AddSingleton<ReceiptPdfService>();

// Student A security services: custom password hashing and email recovery.
builder.Services.AddSingleton<PasswordHashService>();
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Uses a unique anti-forgery cookie so Formal and Practice can run on
// different localhost ports without overwriting each other's form token.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "SportHub.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Builds the database path from the project's runtime root directory.
var databasePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "SportHub.mdf"
);

var connectionString = $"""
    Data Source=(LocalDB)\MSSQLLocalDB;
    Initial Catalog=SportHubDb;
    AttachDbFilename={databasePath};
    Integrated Security=True;
    Connect Timeout=30;
    MultipleActiveResultSets=True;
    TrustServerCertificate=True;
    """;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Registers cookie-based authentication for the application.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Gives the authentication cookie an application-specific name.
        options.Cookie.Name = "SportHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Applies pending Code First migrations before development sample data is checked.
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication determines who the current user is.
app.UseAuthentication();
// Authorization determines what the current user is allowed to do.
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
