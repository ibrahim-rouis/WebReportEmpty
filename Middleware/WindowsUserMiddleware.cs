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
            // Only try to process the Windows user if they are actually logged in
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Try to get the Windows user. This will create a new user in the database if one does not already exist.
                await windowsUserService.GetWindowsUser(context);
            }

            // ALWAYS call the next middleware. 
            // Let the [Authorize] and [AllowAnonymous] attributes handle the security.
            await _next(context);
        }
    }
}