using System.Security.Claims;
using WebReport.Models.Entities;

namespace WebReport.Services
{
    public class WindowsUserService
    {
        private readonly UsersService _usersService;
        private readonly ILogger<WindowsUserService> _logger;
        private readonly RolesService _rolesService;
        public WindowsUserService(UsersService userService, ILogger<WindowsUserService> logger, RolesService rolesService)
        {
            _usersService = userService;
            _logger = logger;
            _rolesService = rolesService;
        }

        // Get Windows user from HttpContext and return the corresponding User entity from database, or null if not found or not authenticated
        // If the Windows user is not authenticated, return null and log a warning
        // if the Windows user is authenticated but not found in database, save it in database and return the User entity
        public async Task<User?> GetWindowsUser(HttpContext httpContext)
        {
            try
            {
                if (httpContext.User == null ||
                    httpContext.User.Identity == null ||
                    httpContext.User.Identity.Name == null
                    )
                {
                    _logger.LogWarning("No authenticated Windows user found in HttpContext");
                    return null;
                }

                var username = httpContext.User.Identity!.Name!;

                // Check if user exists in database
                if (await _usersService.UserNameExists(username))
                {
                    var user = await _usersService.GetUserByName(username);
                    _logger.LogInformation("Windows user {Username} found in database with id={UserId}", username, user?.Id);
                    return user;
                }
                else
                {
                    // Save Windows user in database
                    var createdUser = await SaveWindowsUser(httpContext);

                    if (createdUser == null)
                    {
                        _logger.LogError("Failed to save Windows user {Username} in database", username);
                        return null;
                    }

                    _logger.LogInformation("Windows user {Username} created in database with id={UserId}", username, createdUser.Id);
                    return createdUser;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Windows user from HttpContext");
                return null;
            }
        }

        private async Task<User?> SaveWindowsUser(HttpContext httpContext)
        {

            var username = httpContext.User.Identity!.Name!;

            try
            {
                // Check if user exists in database
                // The GetWindowsUser method should have already checked if the user exists in database, but we check it again
                if (await _usersService.UserNameExists(username))
                {
                    _logger.LogError("Windows user {Username} already exists in database", username);
                    throw new Exception($"Windows user {username} already exists in database");
                }

                /* Get user roles and save new ones in database */

                // Get all role claims for the current user
                var roles = httpContext.User.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                // Roles Ids to assign to user
                List<int> userRolesIds = new List<int>();

                if (roles != null && roles.Count > 0)
                {
                    foreach (var rolename in roles)
                    {
                        // Check if null
                        if (rolename == null)
                        {
                            continue;
                        }

                        // check if role name already exists?
                        if (await _rolesService.RoleNameExists(rolename))
                        {
                            Role? role = await _rolesService.GetRoleByName(rolename);
                            if (role != null)
                            {
                                userRolesIds.Add(role.Id);
                            }
                        }
                        else
                        {
                            // create role in database
                            Role role = await _rolesService.CreateRole(new Role { Name = rolename });
                            userRolesIds.Add(role.Id);
                        }

                    }
                }

                // Save Windows user in database
                var createdUser = await _usersService.CreateUser(new User { Name = username }, userRolesIds.ToArray());
                _logger.LogInformation("Windows user {Username} created in database with id={UserId}", username, createdUser.Id);
                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Windows user {Username} in database", username);
                throw;
            }
        }
    }
}
