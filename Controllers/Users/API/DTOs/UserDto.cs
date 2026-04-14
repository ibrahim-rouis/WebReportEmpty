using System.ComponentModel.DataAnnotations;

namespace WebReport.Controllers.API.DTOs
{
    /// <summary>
    /// User data transfer object for API responses
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Unique identifier of the user
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// Name of the user
        /// </summary>
        /// <example>John Doe</example>
        public string? Name { get; set; }

        /// <summary>
        /// Date and time when the user was last updated
        /// </summary>
        /// <example>2024-06-01T12:34:56Z</example>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// Date and time when the user was created
        /// </summary>
        /// <example>2024-06-01T12:34:56Z</example>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// List of role IDs associated with the user
        /// </summary>
        public List<int>? RoleIds { get; set; }

        /// <summary>
        /// List of role names associated with the user
        /// </summary>
        public List<string>? RoleNames { get; set; }

        /// <summary>
        /// HATEOAS links for this resource
        /// </summary>
        public List<LinkDto> Links { get; set; } = [];
    }

    /// <summary>
    /// Request model for creating a new user
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>
        /// Name of the user (required)
        /// </summary>
        /// <example>John Doe</example>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional list of role IDs to associate with the user
        /// </summary>
        public List<int>? RoleIds { get; set; }
    }

    /// <summary>
    /// Request model for updating an existing user
    /// </summary>
    public class UpdateUserRequest
    {
        /// <summary>
        /// Updated name of the user (required)
        /// </summary>
        /// <example>Jane Doe</example>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Updated list of role IDs to associate with the user
        /// </summary>
        public List<int>? RoleIds { get; set; }
    }
}
