using System.ComponentModel.DataAnnotations;

namespace WebReport.Controllers.API.DTOs
{
    /// <summary>
    /// Role data transfer object for API responses
    /// </summary>
    public class RoleDto
    {
        /// <summary>
        /// Unique identifier of the role
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// Name of the role
        /// </summary>
        /// <example>Administrator</example>
        public string? Name { get; set; }

        /// <summary>
        /// Date and time when the role was created
        /// </summary>
        /// <example>2024-06-01T12:34:56Z</example>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Number of users associated with this role
        /// </summary>
        /// <example>5</example>
        public int UserCount { get; set; }

        /// <summary>
        /// HATEOAS links for this resource
        /// </summary>
        public List<LinkDto> Links { get; set; } = [];
    }

    /// <summary>
    /// Detailed Role response including associated users
    /// </summary>
    public class RoleDetailDto : RoleDto
    {
        /// <summary>
        /// List of user IDs associated with this role
        /// </summary>
        public List<int>? UserIds { get; set; }

        /// <summary>
        /// List of user names associated with this role
        /// </summary>
        public List<string>? UserNames { get; set; }
    }

    /// <summary>
    /// Request model for creating a new role
    /// </summary>
    public class CreateRoleRequest
    {
        /// <summary>
        /// Name of the Role (required)
        /// </summary>
        /// <example>Administrator</example>
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Role name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for updating an existing role
    /// </summary>
    public class UpdateRoleRequest
    {
        /// <summary>
        /// Updated name of the role (required)
        /// </summary>
        /// <example>Super Administrator</example>
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Role name must be between 1 and 100 characters")]
        public string Name { get; set; } = string.Empty;
    }
}
