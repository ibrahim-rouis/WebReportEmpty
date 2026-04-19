using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
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
    // Development uses Cookies (The HTML Login Form)
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

    // Prevent redirect to login/access-denied pages for API requests
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


builder.Services.AddAuthorization(options =>
{
    // Require an authenticated user (via Cookies)
    // This will trigger a redirect to /Account/Login if the user isn't logged in
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- LDAP ---
builder.Services.AddScoped<LdapService>(); // Your helper for LDAP

builder.Services.AddMemoryCache();
builder.Services.AddTransient<IClaimsTransformation, LdapClaimsTransformer>();

// App Services
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<RolesService>();
builder.Services.AddScoped<LogViewerService>();
builder.Services.AddScoped<WindowsUserService>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "WebReport API",
        Description = "A HATEOAS-driven REST API for managing users and profiles in the WebReport application.",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "WebReport Support",
            Email = "support@webreport.com",
            Url = new Uri("https://example.com/support")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add your base URL(s) to the Swagger JSON
    options.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
    {
        Url = "https://localhost:7891",
        Description = "Local Development Server"
    });

    // Include XML comments for API documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add custom tags for better organization
    options.TagActionsBy(api =>
    {
        if (api.GroupName != null)
        {
            return [api.GroupName];
        }

        if (api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerActionDescriptor)
        {
            return [controllerActionDescriptor.ControllerName];
        }

        return ["Misc"];
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Enable Swagger in all environments
app.UseSwagger(options =>
{
    options.RouteTemplate = "api/swagger/{documentName}/swagger.json";
});

// link will be at /api/docs/index.html
// json will be at /api/swagger/v1/swagger.json
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/api/swagger/v1/swagger.json", "WebReport API v1");
    options.RoutePrefix = "api/docs";
    options.DocumentTitle = "WebReport API Documentation";
    options.DefaultModelsExpandDepth(2);
    options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    options.EnableDeepLinking();
    options.DisplayOperationId();
    options.EnableTryItOutByDefault();
});

// Only use HTTPS redirection if an HTTPS port is bound (e.g., in Production with a real cert)
if (!app.Environment.IsDevelopment())
{
    // ... your HSTS code
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
