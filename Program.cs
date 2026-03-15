using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebReport.Configuration;
using WebReport.Models;
using WebReport.Services;
using WebReport.Services.LDAP;

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for all logging
builder.Services.AddSerilog((services, lc) => lc
.ReadFrom.Configuration(builder.Configuration)
.ReadFrom.Services(services));

// Register the DbContext with the connection string and MySQL provider
var connectionString = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<WebReportDBContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Bind WebReportConfig from appsettings.json to the WebReportConfig class and make it available for injection
builder.Services.Configure<WebReportConfig>(builder.Configuration.GetSection("WebReportConfig"));

// Bind LdapConfig from appsettings.json
builder.Services.Configure<LdapConfig>(builder.Configuration.GetSection("LdapConfig"));

// --- Authentication Setup ---
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

// Register the Cookie handler (used for your LDAP form)
authBuilder.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30); // Persistent for 30 days
    options.SlidingExpiration = true;
});

// Register Negotiate for everyone so the services are available
authBuilder.AddNegotiate();

/* ------------------------------- */

builder.Services.AddAuthorization(options =>
{
    // Require an authenticated user (via Cookies)
    // This will trigger a redirect to /Account/Login if the user isn't logged in
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- LDAP ---
builder.Services.AddSingleton<LdapService>(); // Your helper for LDAP

// App Services
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<RolesService>();
builder.Services.AddScoped<LogViewerService>();
builder.Services.AddScoped<WindowsUserService>();

builder.Services.AddRazorPages();

var app = builder.Build();

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

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
