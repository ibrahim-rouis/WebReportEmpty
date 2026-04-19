using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebReport.Models.ViewModels;
using WebReport.Services;
using WebReport.Services.LDAP;

namespace WebReport.Controllers.Users
{
    [Route("Account")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly WindowsUserService _windowsUserService;

        private const string defaultAvatarPath = "~/img/avatars/default.svg";

        public AccountController(LdapService ldapService, ILogger<AccountController> logger, WindowsUserService windowsUserService)
        {
            _logger = logger;
            _windowsUserService = windowsUserService;
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning("Access denied to {Path} for user {User}", HttpContext.Request.Path, User.Identity?.Name);
            return View("~/Views/UsersMgr/Account/AccessDenied.cshtml");
        }

        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            _logger.LogInformation("Accessing Account/Login");

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/UsersMgr/Account/Login.cshtml");
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        // This is a simple login form that validates against the Docker LDAP server.
        // This is intended for development purposes only and will not be used in production.
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                if (await _windowsUserService.LoginUser(HttpContext, model))
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        return Redirect(model.ReturnUrl);

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid LDAP username or password.");
                    return View("~/Views/UsersMgr/Account/Login.cshtml", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Error during login for user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again later.");
                return View("~/Views/UsersMgr/Account/Login.cshtml", model);
            }

        }

        [HttpPost("Logout")]
        [ValidateAntiForgeryToken]
        // This simply clears the authentication cookie. The IClaimsTransformation will handle the rest on the next request.
        public async Task<IActionResult> Logout()
        {
            // This clears the ".AspNetCore.Cookies" cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("User logged out at {Time}", DateTime.UtcNow);

            // Redirect to the Login page or Home
            // In Dev, this will show the login form again.
            return RedirectToAction("Login", "Account");
        }


        [HttpGet("ProfilePicture")]
        public async Task<IActionResult> ProfilePicture()
        {
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
