using Microsoft.AspNetCore.Http;
using WebReport.Services;

namespace WebReport.Middleware
{
    public class WindowsUserMiddleware
    {
        private readonly RequestDelegate _next;

        public WindowsUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, WindowsUserService windowsUserService)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized");
                return; // Block further processing
            }

            await windowsUserService.GetWindowsUser(context);
            await _next(context);
        }
    }
}