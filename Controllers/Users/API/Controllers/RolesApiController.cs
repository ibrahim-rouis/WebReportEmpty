using Microsoft.AspNetCore.Mvc;
using WebReport.Controllers.API.DTOs;
using WebReport.Filters;
using WebReport.Models.Entities;
using WebReport.Services;

namespace WebReport.Controllers.API.Controllers
{
    /// <summary>
    /// API endpoints for managing Roles
    /// </summary>
    /// <remarks>
    /// This controller provides HATEOAS-driven REST API endpoints for profile management
    /// including CRUD operations and pagination support.
    /// </remarks>
    [ApiController]
    [Route("api/roles")]
    [Produces("application/json")]
    [ApiKey]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public class RolesApiController : ControllerBase
    {
        private readonly RolesService _rolesService;
        private readonly ILogger<RolesApiController> _logger;

        /// <summary>
        /// Initializes a new instance of the ProfilsApiController
        /// </summary>
        public RolesApiController(
            RolesService rolesService,
            ILogger<RolesApiController> logger)
        {
            _rolesService = rolesService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a paginated list of roles
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="searchString">Optional search filter for role names</param>
        /// <returns>A paginated list of roles with HATEOAS links</returns>
        /// <response code="200">Returns the paginated list of roles</response>
        /// <response code="400">If the page number is invalid</response>
        [HttpGet(Name = "GetRoles")]
        [ProducesResponseType(typeof(PaginatedResponse<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedResponse<RoleDto>>> GetRoles(
            [FromQuery] int pageNumber = 1,
            [FromQuery] string? searchString = null)
        {
            _logger.LogInformation("API: Retrieving roles list - Page: {PageNumber}, Search: {SearchString}",
                pageNumber, searchString);

            if (pageNumber < 1)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Type = "BadRequest",
                    Title = "Invalid Page Number",
                    Status = 400,
                    Detail = "Page number must be greater than 0.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var roles = await _rolesService.GetRoles(searchString ?? "", pageNumber);

            var roleDtos = roles.Select(r => MapToRoleDto(r)).ToList();

            var response = new PaginatedResponse<RoleDto>
            {
                Items = roleDtos,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = roles.PageIndex,
                    TotalPages = roles.TotalPages,
                    PageSize = roles.Count,
                    TotalCount = roles.TotalPages > 0 ? roles.TotalPages * roles.Count : 0,
                    HasPrevious = roles.HasPreviousPage,
                    HasNext = roles.HasNextPage
                },
                Links = GenerateCollectionLinks(pageNumber, roles.TotalPages, searchString)
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets all roles without pagination
        /// </summary>
        /// <returns>A list of all roles with HATEOAS links</returns>
        /// <response code="200">Returns all roles</response>
        [HttpGet("all", Name = "GetAllRoles")]
        [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            _logger.LogInformation("API: Retrieving all roles");

            var roles = await _rolesService.GetAllRoles();
            var roleDtos = roles.Select(r => MapToRoleDto(r)).ToList();

            return Ok(roleDtos);
        }

        /// <summary>
        /// Gets a specific role by ID
        /// </summary>
        /// <param name="id">The role ID</param>
        /// <returns>The role with HATEOAS links</returns>
        /// <response code="200">Returns the requested role</response>
        /// <response code="404">If the role is not found</response>
        [HttpGet("{id:int}", Name = "GetRoleById")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDto>> GetRole(int id)
        {
            _logger.LogInformation("API: Retrieving role with ID: {RoleId}", id);
            var role = await _rolesService.GetRoleById(id);

            if (role == null)
            {
                _logger.LogWarning("API: Role with ID {RoleId} not found", id);
                return NotFound(new ApiErrorResponse
                {
                    Type = "NotFound",
                    Title = "Role Not Found",
                    Status = 404,
                    Detail = $"The role with ID {id} was not found.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var roleDto = MapToRoleDto(role);
            return Ok(roleDto);
        }

        /// <summary>
        /// Gets a specific profile by ID with associated users
        /// </summary>
        /// <param name="id">The profile ID</param>
        /// <returns>The profile with user details and HATEOAS links</returns>
        /// <response code="200">Returns the requested profile with users</response>
        /// <response code="404">If the profile is not found</response>
        [HttpGet("{id:int}/details", Name = "GetRoleDetails")]
        [ProducesResponseType(typeof(RoleDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDetailDto>> GetRoleDetails(int id)
        {
            _logger.LogInformation("API: Retrieving role details with ID: {RoleId}", id);
            var role = await _rolesService.GetRoleByIdWithUsers(id);

            if (role == null)
            {
                _logger.LogWarning("API: Role with ID {RoleId} not found", id);
                return NotFound(new ApiErrorResponse
                {
                    Type = "NotFound",
                    Title = "Role Not Found",
                    Status = 404,
                    Detail = $"The role with ID {id} was not found.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var roleDetailDto = MapToRoleDetailDto(role);
            return Ok(roleDetailDto);
        }

        /// <summary>
        /// Creates a new profile
        /// </summary>
        /// <param name="request">The profile creation request</param>
        /// <returns>The created profile with HATEOAS links</returns>
        /// <response code="201">Returns the newly created profile</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost(Name = "CreateRole")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
        {
            _logger.LogInformation("API: Creating new role: {RoleName}", request.Name);
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Type = "ValidationError",
                    Title = "Validation Failed",
                    Status = 400,
                    Detail = "One or more validation errors occurred.",
                    TraceId = HttpContext.TraceIdentifier,
                    Errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? [])
                });
            }

            var profil = new Role
            {
                Name = request.Name
            };

            await _rolesService.CreateRole(profil);
            _logger.LogInformation("API: Successfully created role with ID: {RoleId}", profil.Id);

            var roleDto = MapToRoleDto(profil);

            return CreatedAtRoute("GetRoleById", new { id = profil.Id }, roleDto);
        }

        /// <summary>
        /// Updates an existing role
        /// </summary>
        /// <param name="id">The role ID to update</param>
        /// <param name="request">The role update request</param>
        /// <returns>The updated role with HATEOAS links</returns>
        /// <response code="200">Returns the updated role</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="404">If the role is not found</response>
        [HttpPut("{id:int}", Name = "UpdateRole")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            _logger.LogInformation("API: Updating role with ID: {RoleId}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiErrorResponse
                {
                    Type = "ValidationError",
                    Title = "Validation Failed",
                    Status = 400,
                    Detail = "One or more validation errors occurred.",
                    TraceId = HttpContext.TraceIdentifier,
                    Errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? [])
                });
            }

            var role = await _rolesService.GetRoleById(id);

            if (role == null)
            {
                _logger.LogWarning("API: Role with ID {RoleId} not found for update", id);
                return NotFound(new ApiErrorResponse
                {
                    Type = "NotFound",
                    Title = "Role Not Found",
                    Status = 404,
                    Detail = $"The role with ID {id} was not found.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            role.Name = request.Name;

            await _rolesService.UpdateRole(role);
            _logger.LogInformation("API: Successfully updated role with ID: {RoleId}", id);

            var roleDto = MapToRoleDto(role);
            return Ok(roleDto);
        }

        /// <summary>
        /// Deletes a role
        /// </summary>
        /// <param name="id">The role ID to delete</param>
        /// <returns>No content on success</returns>
        /// <response code="204">Role was successfully deleted</response>
        /// <response code="404">If the role is not found</response>
        /// <remarks>
        /// Note: Deleting a role will remove the role association from all users
        /// that have this role assigned, but will not delete the users themselves.
        /// </remarks>
        [HttpDelete("{id:int}", Name = "DeleteRole")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRole(int id)
        {
            _logger.LogInformation("API: Deleting role with ID: {RoleId}", id);

            var role = await _rolesService.GetRoleByIdWithUsers(id);
            if (role == null)
            {
                _logger.LogWarning("API: Role with ID {RoleId} not found for deletion", id);
                return NotFound(new ApiErrorResponse
                {
                    Type = "NotFound",
                    Title = "Role Not Found",
                    Status = 404,
                    Detail = $"The role with ID {id} was not found.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            await _rolesService.DeleteRole(role);
            _logger.LogInformation("API: Successfully deleted role with ID: {RoleId}", id);

            return NoContent();
        }

        #region Private Helper Methods

        private RoleDto MapToRoleDto(Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                UserCount = role.Users?.Count ?? 0,
                Links = GenerateRoleLinks(role.Id)
            };
        }

        private RoleDetailDto MapToRoleDetailDto(Role role)
        {
            return new RoleDetailDto
            {
                Id = role.Id,
                Name = role.Name,
                UserCount = role.Users?.Count ?? 0,
                UserIds = role.Users?.Select(u => u.Id).ToList(),
                UserNames = role.Users?.Select(u => u.Name ?? "").ToList(),
                Links = GenerateRoleLinks(role.Id)
            };
        }

        private List<LinkDto> GenerateRoleLinks(int roleId)
        {
            return
            [
                new LinkDto(
                    Url.Link("GetRoleById", new { id = roleId }) ?? "",
                    "self",
                    "GET"),
                new LinkDto(
                    Url.Link("GetRoleDetails", new { id = roleId }) ?? "",
                    "details",
                    "GET"),
                new LinkDto(
                    Url.Link("UpdateRole", new { id = roleId }) ?? "",
                    "update",
                    "PUT"),
                new LinkDto(
                    Url.Link("DeleteRole", new { id = roleId }) ?? "",
                    "delete",
                    "DELETE"),
                new LinkDto(
                    Url.Link("GetRoles", null) ?? "",
                    "collection",
                    "GET")
            ];
        }

        private List<LinkDto> GenerateCollectionLinks(int currentPage, int totalPages, string? searchString)
        {
            var links = new List<LinkDto>
            {
                new(
                    Url.Link("GetRoles", new { pageNumber = currentPage, searchString }) ?? "",
                    "self",
                    "GET"),
                new(
                    Url.Link("CreateRole", null) ?? "",
                    "create",
                    "POST"),
                new(
                    Url.Link("GetAllRoles", null) ?? "",
                    "all",
                    "GET"),
                new(
                    Url.Link("GetRoles", new { pageNumber = 1, searchString }) ?? "",
                    "first",
                    "GET")
            };

            if (totalPages > 0)
            {
                links.Add(new LinkDto(
                    Url.Link("GetRoles", new { pageNumber = totalPages, searchString }) ?? "",
                    "last",
                    "GET"));
            }

            if (currentPage > 1)
            {
                links.Add(new LinkDto(
                    Url.Link("GetRoles", new { pageNumber = currentPage - 1, searchString }) ?? "",
                    "previous",
                    "GET"));
            }

            if (currentPage < totalPages)
            {
                links.Add(new LinkDto(
                    Url.Link("GetRoles", new { pageNumber = currentPage + 1, searchString }) ?? "",
                    "next",
                    "GET"));
            }

            return links;
        }

        #endregion
    }
}
