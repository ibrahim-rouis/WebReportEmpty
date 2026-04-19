namespace WebReport.Controllers.Users.API.DTOs
{
    /// <summary>
    /// Represents the data transfer object for a login request.
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Gets or sets the username provided by the user.
        /// </summary>
        /// <example>jdoe</example>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password provided by the user.
        /// </summary>
        /// <example>SuperSecretPassword123!</example>
        public string Password { get; set; } = string.Empty;
    }
}