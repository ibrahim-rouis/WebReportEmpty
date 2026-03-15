using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebReport.Configuration;
using WebReport.Middleware;
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
    if (builder.Environment.IsDevelopment())
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }
    else
    {
        options.DefaultScheme = NegotiateDefaults.AuthenticationScheme;
    }
});

// Register the Cookie handler (used for your LDAP form)
authBuilder.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

// Register Negotiate for everyone so the services are available
authBuilder.AddNegotiate();

/* ------------------------------- */

builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // DEVELOPMENT: Require an authenticated user (via Cookies)
        // This will trigger a redirect to /Account/Login if the user isn't logged in
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
    else
    {
        // PRODUCTION: Require a Windows User (via Negotiate)
        options.FallbackPolicy = options.DefaultPolicy;
    }
});

// --- LDAP ---
builder.Services.AddSingleton<LdapService>(); // Your helper for Docker LDAP
builder.Services.AddScoped<IClaimsTransformation, LdapClaimsTransformer>(); // The "Bridge"

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

// This middleware will run on every request and attempt to get the Windows user from HttpContext.
app.UseMiddleware<WindowsUserMiddleware>();

app.Run();
