using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
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
        private readonly LdapService _ldapService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(LdapService ldapService, ILogger<AccountController> logger)
        {
            _ldapService = ldapService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login()
        {
            _logger.LogInformation("Accessing Account/Login");

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

            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Username and password are required.");
                return View(model);
            }

            // 1. Validate against Docker LDAP
            if (_ldapService.ValidateUserCredentials(model.Username, model.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    // We don't add roles here; the IClaimsTransformation will do it automatically!
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid LDAP username or password.");
            return View("~/Views/UsersMgr/Account/Login.cshtml", model);
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
    }
}
