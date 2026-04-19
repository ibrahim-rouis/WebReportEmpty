using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebReport.Configuration;
using WebReport.Controllers.API.Controllers;
using WebReport.Controllers.Users.API.DTOs;
using WebReport.Models.ViewModels;
using WebReport.Services;

namespace WebReportEmpty.Controllers.Users.API.Controllers
{
    /// <summary>
    /// Handles account-related API operations such as login, logout, and retrieving profile information.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [ApiKey]
    [Produces("application/json")]
    public class AccountApiController : ControllerBase
    {
        // Inject your authentication or user services here
        private readonly WindowsUserService _windowsUserService;
        private readonly ILogger<RolesApiController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountApiController"/> class.
        /// </summary>
        public AccountApiController(WindowsUserService windowsUserService, ILogger<RolesApiController> logger)
        {
            _windowsUserService = windowsUserService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticates a user and generates a session or token.
        /// </summary>
        /// <param name="request">The login credentials (username and password).</param>
        /// <returns>A success message and authentication token if valid; otherwise, an unauthorized error.</returns>
        /// <response code="200">Returns successful login message and token.</response>
        /// <response code="400">If the provided model is invalid or missing fields.</response>
        /// <response code="401">If the username or password is incorrect.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            LoginViewModel model = new LoginViewModel();
            model.Username = request.Username;
            model.Password = request.Password;

            if (await _windowsUserService.LoginUser(HttpContext, model))
            {
                return Ok(new { message = "Login successful" });
            }

            return Unauthorized(new { message = "Invalid username or password." });
        }

        /// <summary>
        /// Logs out the currently authenticated user and invalidates their session/token.
        /// </summary>
        /// <returns>A message confirming successful logout.</returns>
        /// <response code="200">Successfully logged out.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            // This clears the ".AspNetCore.Cookies" cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Retrieves the profile information of the currently authenticated user.
        /// </summary>
        /// <returns>The user's profile details.</returns>
        /// <response code="200">Returns the user's profile information.</response>
        /// <response code="401">If the user is not authenticated.</response>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            var username = User.Identity!.Name;

            var user = await _windowsUserService.GetWindowsUserByName(username!);

            if (user == null)
            {
                throw new KeyNotFoundException($"User '{username}' was not found in the database.");
            }

            return Ok(new
            {
                username = user.Name,
                fullname = user.FullName,
                createdAt = user.CreatedAt,
            });
        }
    }
}