using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebReport.Filters
{

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAttribute : Attribute, IAsyncActionFilter
    {
        private const string ApiKeyHeaderName = "X-Api-Key";
        private const string ApiKeyConfigName = "ApiKey";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1. Check if the header exists
            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key is missing from headers.");
                return;
            }

            // 2. Get the configured API Key
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var configuredApiKey = configuration.GetValue<string>(ApiKeyConfigName);

            // 3. Validate
            if (string.IsNullOrEmpty(configuredApiKey) || !configuredApiKey.Equals(extractedApiKey))
            {
                context.Result = new UnauthorizedObjectResult("Invalid API Key.");
                return;
            }

            await next();
        }
    }
}
