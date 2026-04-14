using Microsoft.AspNetCore.Mvc;
using WebReport.Controllers.API.DTOs;
using WebReport.Filters;
using WebReport.Models.Entities;
using WebReport.Services;

namespace WebReport.Controllers.API.Controllers
{
    /// <summary>
    /// API endpoints for managing users
    /// </summary>
    /// <remarks>
    /// This controller provides HATEOAS-driven REST API endpoints for user management
    /// including CRUD operations and pagination support.
    /// </remarks>
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    [ApiKey]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public class UsersApiController : ControllerBase
    {
        private readonly UsersService _usersService;
        private readonly RolesService _rolesService;
        private readonly ILogger<UsersApiController> _logger;

        /// <summary>
        /// Initializes a new instance of the UsersApiController
        /// </summary>
        public UsersApiController(
            UsersService usersService,
            RolesService rolesService,
            ILogger<UsersApiController> logger)
        {
            _usersService = usersService;
            _rolesService = rolesService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a paginated list of users
        /// </summary>
        /// <param name="pageNumber">Page number (default: 1)</param>
        /// <param name="searchString">Optional search filter for user names</param>
        /// <param name="roleId">Optional filter by Role ID</param>
        /// <returns>A paginated list of users with HATEOAS links</returns>
        /// <response code="200">Returns the paginated list of users</response>
        /// <response code="400">If the page number is invalid</response>
        [HttpGet(Name = "GetUsers")]
        [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] string? searchString = null,
            [FromQuery] int? roleId = null)
        {
            _logger.LogInformation("API: Retrieving users list - Page: {PageNumber}, Search: {SearchString}, RoleId: {RoleId}",
                pageNumber, searchString, roleId);

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

            var users = await _usersService.GetUsers(searchString ?? "", pageNumber, roleId);

            var userDtos = users.Select(u => MapToUserDto(u)).ToList();

            var response = new PaginatedResponse<UserDto>
            {
                Items = userDtos,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = users.PageIndex,
                    TotalPages = users.TotalPages,
                    PageSize = users.Count,
                    TotalCount = users.TotalPages > 0 ? users.TotalPages * users.Count : 0,
                    HasPrevious = users.HasPreviousPage,
                    HasNext = users.HasNextPage
                },
                Links = GenerateCollectionLinks(pageNumber, users.TotalPages, searchString, roleId)
            };

            return Ok(response);
        }

        /// <summary>
        /// Gets a specific user by ID
        /// </summary>
        /// <param name="id">The user ID</param>
        /// <returns>The user with HATEOAS links</returns>
        /// <response code="200">Returns the requested user</response>
        /// <response code="404">If the user is not found</response>
        [HttpGet("{id:int}", Name = "GetUserById")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            _logger.LogInformation("API: Retrieving user with ID: {UserId}", id);

            var user = await _usersService.GetUserById(id);

            if (user == null)
            {
                _logger.LogWarning("API: User with ID {UserId} not found", id);
                return NotFound(new ApiErrorResponse
                {
                    Type = "NotFound",
                    Title = "User Not Found",
                    Status = 404,
                    Detail = $"The user with ID {id} was not found.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            var userDto = MapToUserDto(user);
            return Ok(userDto);
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        /// <param name="request">The user creation request</param>
        /// <returns>The created user with HATEOAS links</returns>
        /// <response code="201">Returns the newly created user</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost(Name = "CreateUser")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
        {
            _logger.LogInformation("API: Creating new user: {UserName}", request.Name);

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

            var user = new User
            {
                Name = request.Name
            };

            await _usersService.CreateUser(user, request.RoleIds?.ToArray() ?? []);
            _logger.LogInformation("API: Successfully created user with ID: {UserId}", user.Id);

            var userDto = MapToUserDto(user);

            return CreatedAtRoute("GetUserById", new { id = user.Id }, userDto);
        }

        /// <summary>
        /// Updates an existing user
        /// </summary>
        /// <param name="id">The user ID to update</param>
        /// <param name="request">The user update request</param>
        /// <returns>The updated user with HATEOAS links</returns>
        /// <response code="200">Returns the updated user</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="404">If the user is not found</response>
        [HttpPut("{id:int}", Name = "UpdateUser")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            _logger.LogInformation("API: Updating user with ID: {UserId}", id);

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

            var user = await _usersService.GetUserById(id);

            if (user == null)
            {
                _logger.LogWarning("API: User with ID {UserId} not found for update", id);
                return NotFound(new ApiErrorResponse
                {
                    Type = "NotFound",
                    Title = "User Not Found",
                    Status = 404,
                    Detail = $"The user with ID {id} was not found.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            user.Name = request.Name;

            await _usersService.UpdateUserRoles(user.Id, user, request.RoleIds?.ToArray() ?? []);
            _logger.LogInformation("API: Successfully updated user with ID: {UserId}", id);

            var userDto = MapToUserDto(user);
            return Ok(userDto);
        }

        /// <summary>
        /// Deletes a user
        /// </summary>
        /// <param name="id">The user ID to delete</param>
        /// <returns>No content on success</returns>
        /// <response code="204">User was successfully deleted</response>
        /// <response code="404">If the user is not found</response>
        [HttpDelete("{id:int}", Name = "DeleteUser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            _logger.LogInformation("API: Deleting user with ID: {UserId}", id);

            var exists = await _usersService.UserExists(id);

            if (!exists)
            {
                _logger.LogWarning("API: User with ID {UserId} not found for deletion", id);
                return NotFound(new ApiErrorResponse
                {
                    Type = "NotFound",
                    Title = "User Not Found",
                    Status = 404,
                    Detail = $"The user with ID {id} was not found.",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            await _usersService.DeleteUser(id);
            _logger.LogInformation("API: Successfully deleted user with ID: {UserId}", id);

            return NoContent();
        }

        #region Private Helper Methods

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                RoleIds = user.Roles?.Select(r => r.Id).ToList(),
                RoleNames = user.Roles?.Select(r => r.Name ?? "").ToList(),
                Links = GenerateUserLinks(user.Id)
            };
        }

        private List<LinkDto> GenerateUserLinks(int userId)
        {
            return
            [
                new LinkDto(
                    Url.Link("GetUserById", new { id = userId }) ?? "",
                    "self",
                    "GET"),
                new LinkDto(
                    Url.Link("UpdateUser", new { id = userId }) ?? "",
                    "update",
                    "PUT"),
                new LinkDto(
                    Url.Link("DeleteUser", new { id = userId }) ?? "",
                    "delete",
                    "DELETE"),
                new LinkDto(
                    Url.Link("GetUsers", null) ?? "",
                    "collection",
                    "GET")
            ];
        }

        private List<LinkDto> GenerateCollectionLinks(int currentPage, int totalPages, string? searchString, int? roleId)
        {
            var links = new List<LinkDto>
            {
                new(
                    Url.Link("GetUsers", new { pageNumber = currentPage, searchString, roleId }) ?? "",
                    "self",
                    "GET"),
                new(
                    Url.Link("CreateUser", null) ?? "",
                    "create",
                    "POST"),
                new(
                    Url.Link("GetUsers", new { pageNumber = 1, searchString, roleId }) ?? "",
                    "first",
                    "GET")
            };

            if (totalPages > 0)
            {
                links.Add(new LinkDto(
                    Url.Link("GetUsers", new { pageNumber = totalPages, searchString, roleId }) ?? "",
                    "last",
                    "GET"));
            }

            if (currentPage > 1)
            {
                links.Add(new LinkDto(
                    Url.Link("GetUsers", new { pageNumber = currentPage - 1, searchString, roleId }) ?? "",
                    "previous",
                    "GET"));
            }

            if (currentPage < totalPages)
            {
                links.Add(new LinkDto(
                    Url.Link("GetUsers", new { pageNumber = currentPage + 1, searchString, roleId }) ?? "",
                    "next",
                    "GET"));
            }

            return links;
        }

        #endregion
    }
}
