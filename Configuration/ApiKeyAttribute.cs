using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebReport.Configuration
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string APIKEYNAME = "X-Api-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "API Key was not provided."
                };
                return;
            }

            // Resolve IConfiguration from the service container
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            // Get the API key from appsettings.json (e.g., "ApiKey": "your-secret-key")
            var expectedApiKey = configuration.GetValue<string>("ApiKey");

            if (!string.Equals(expectedApiKey, extractedApiKey.ToString()))
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 401,
                    Content = "Unauthorized client."
                };
                return;
            }

            await next();
        }
    }
}