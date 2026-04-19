using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
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
    if (builder.Environment.IsDevelopment())
    {
        // Development uses Cookies (The HTML Login Form)
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    }
    else
    {
        options.DefaultScheme = NegotiateDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = NegotiateDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = NegotiateDefaults.AuthenticationScheme;
    }
});

authBuilder.AddNegotiate();

// Register the Cookie handler (used for your LDAP form)
if (builder.Environment.IsDevelopment())
{
    authBuilder.AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Persistent for 30 days
        options.SlidingExpiration = true;
    });
}

builder.Services.AddAuthorization(options =>
{
    // Require an authenticated user (via Cookies)
    // This will trigger a redirect to /Account/Login if the user isn't logged in
    if (builder.Environment.IsDevelopment())
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
    else
    {
        options.FallbackPolicy = options.DefaultPolicy;
    }
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

    // Define the API Key security scheme
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. Enter your API Key below.",
        In = ParameterLocation.Header,
        Name = "X-Api-Key", // Must match the header name in your ApiKeyAttribute
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    // Apply the API Key requirement to all endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                Scheme = "ApiKeyScheme",
                Name = "ApiKey",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
