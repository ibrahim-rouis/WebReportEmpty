using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebReport.Models.ViewModels;
using WebReport.Services;
using WebReport.Services.LDAP;

// This is used for development purpose only
// It provides a simple login form that validates against the Docker LDAP server.
// In production, we will rely on Windows Authentication and the IClaimsTransformation to automatically populate user roles and photos from LDAP.
// Ignore this controller for now, it will not be used in production and is only intended to make development easier without needing to set up Windows Authentication on the dev machine.
namespace WebReport.Controllers.Users
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly WindowsUserService _windowsUserService;

        public AccountController(WindowsUserService windowsUserService)
        {
            _windowsUserService = windowsUserService;
        }

        [HttpGet("ProfilePicture")]
        public async Task<IActionResult> ProfilePicture()
        {
            var defaultAvatarPath = "~/img/avatars/default.svg";

            if (User.Identity != null && User.Identity.IsAuthenticated && !string.IsNullOrEmpty(User.Identity.Name))
            {
                // Fetch from Database (Super Fast) instead of LDAP
                var user = await _windowsUserService.GetWindowsUserByName(User.Identity.Name);

                if (user?.Photo != null)
                {
                    return File(user.Photo, "image/jpeg");
                }
            }

            // Return a default "avatar" icon if no photo exists
            return LocalRedirect(Url.Content(defaultAvatarPath)!);
        }
    }
}
