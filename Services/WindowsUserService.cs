using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using WebReport.Models.Entities;
using WebReport.Models.ViewModels;
using WebReport.Services.LDAP;

namespace WebReport.Services
{
    public class WindowsUserService
    {
        private readonly UsersService _usersService;
        private readonly LdapService _ldapService;
        private readonly ILogger<WindowsUserService> _logger;
        private readonly RolesService _rolesService;
        public WindowsUserService(UsersService userService, ILogger<WindowsUserService> logger, RolesService rolesService, LdapService ldapService)
        {
            _usersService = userService;
            _logger = logger;
            _rolesService = rolesService;
            _ldapService = ldapService;
        }

        // Get Windows user from HttpContext and return the corresponding User entity from database, or null if not found.
        private async Task<User?> GetWindowsUser(HttpContext httpContext)
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
                    _logger.LogWarning("Windows user {Username} not found in database.", username);
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Something fatal happened went wrong and need to be thrown
                // This should not happen, but we log it just in case
                _logger.LogError(ex, "Error getting Windows user from HttpContext");
                throw;
            }
        }

        public async Task<User?> GetWindowsUserByName(string username)
        {
            try
            {
                // Check if user exists in database
                if (await _usersService.UserNameExists(username))
                {
                    var user = await _usersService.GetUserByName(username);
                    return user;
                }
                else
                {
                    _logger.LogWarning("Windows user {Username} not found in database.", username);
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Something fatal happened went wrong and need to be thrown
                // This should not happen, but we log it just in case
                _logger.LogError(ex, "Error getting Windows user from HttpContext");
                throw;
            }
        }

        public async Task<bool> LoginUser(HttpContext httpContext, LoginViewModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Password))
            {
                return false;
            }
            try
            {
                // 1. Validate against Docker LDAP
                if (_ldapService.ValidateUserCredentials(model.Username, model.Password))
                {
                    var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            // We don't add roles here; the IClaimsTransformation will do it automatically!
                        };

                    // Fetch groups from LDAP
                    var ldapGroups = _ldapService.GetUserGroups(model.Username);

                    // Map LDAP groups to Application Roles
                    foreach (var group in ldapGroups)
                    {
                        // You can add the group exactly as it is in LDAP
                        claims.Add(new Claim(ClaimTypes.Role, group));

                        // Or map specific LDAP groups to friendly local names
                        /*
                        if (group == "LEONI_IT_ADMINS") 
                            newIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                        */
                    }

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Save user to database if not exists
                    if (!await SaveUserToDbIfNotExist(model.Username, ldapGroups))
                    {
                        _logger.LogError("Error during Windows user login save {Username}", model.Username);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Windows user login for {Username}", model.Username);
                throw;
            }
        }

        private async Task<bool> SaveUserToDbIfNotExist(string username, List<string> ldapGroups)
        {
            try
            {
                var user = await GetWindowsUserByName(username);
                if (user != null)
                {
                    _logger.LogInformation("Windows user {Username} already exists in database with id={UserId}, no need to save.", user.Name, user.Id);
                    return true;
                }

                var createdUser = await SaveWindowsUser(username, ldapGroups);

                return createdUser != null;
            }
            catch (Exception ex)
            {
                // Something fatal happened went wrong and need to be thrown
                // This should not happen, but we log it just in case
                _logger.LogError(ex, "Error during Windows user save");
                throw;
            }
        }

        public async Task<User?> SaveWindowsUser(string username, List<string> ldapGroups)
        {
            try
            {
                // Check if user exists in database
                // The SaveUserToDbIfNotExist method should have already checked if the user exists in database, but we check it again
                if (await _usersService.UserNameExists(username))
                {
                    _logger.LogError("Windows user {Username} already exists in database, we can't save it twice.", username);
                    throw new Exception($"Windows user {username} already exists in database");
                }

                /* Get user roles and save new ones in database */


                // Roles Ids to assign to user
                List<int> userRolesIds = new List<int>();

                if (ldapGroups != null && ldapGroups.Count > 0)
                {
                    foreach (var rolename in ldapGroups)
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

                // Get UserPhoto from LDAP
                var userPhotoBytes = _ldapService.GetUserPhoto(username);

                var newUser = new User
                {
                    Name = username,
                    Photo = userPhotoBytes, // We don't have user photo from Windows authentication, so we set it to null
                };

                // Save Windows user in database
                var createdUser = await _usersService.CreateUser(newUser, userRolesIds.ToArray());
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
