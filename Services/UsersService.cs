using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebReport.Configuration;
using WebReport.Controllers;
using WebReport.Models;
using WebReport.Models.Common;
using WebReport.Models.Entities;

namespace WebReport.Services
{
    public class UsersService
    {
        private readonly WebReportDBContext _context;
        private readonly WebReportConfig _config;
        private readonly ILogger<UsersService> _logger;
        private readonly RolesService _rolesService;


        public UsersService(WebReportDBContext context, IOptions<WebReportConfig> webReportConfig, ILogger<UsersService> logger, RolesService rolesService)
        {
            _context = context;
            _config = webReportConfig.Value;
            _logger = logger;
            _rolesService = rolesService;
        }

        public async Task<PaginatedList<User>> GetUsers(string searchString, int pageIndex, int? roleIdFilter)
        {
            _logger.LogInformation("Getting users with searchString: {searchString}, pageIndex: {pageIndex}, roleIdFilter: {roleIdFilter}", searchString, pageIndex, roleIdFilter);

            // NoTracking is recommended to make pages consistent when something changes during viewing it
            var users = _context.Users.Include(u => u.Roles).AsNoTracking();

            // Apply search filer if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Name!.ToUpper().Contains(searchString.ToUpper()));
            }
            if (roleIdFilter.HasValue)
            {
                users = users.Where(u => u.Roles!.Any(p => p.Id == roleIdFilter));

            }

            // Add OrderBy to ensure consistent ordering of results
            users = users.OrderBy(u => u.Id);

            return await PaginatedList<User>.CreateAsync(users, pageIndex, _config.DefaultPageSize);
        }

        public async Task<User?> GetUserById(int id)
        {
            _logger.LogInformation("Getting user by id: {id}", id);
            return await _context.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task CreateUser(User user)
        {
            _logger.LogInformation("Creating user with name: {name}", user.Name);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUser(User user)
        {
            _logger.LogInformation("Updating user with id: {id}", user.Id);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUser(int id)
        {
            _logger.LogInformation("Deleting user with id: {id}", id);
            var user = await this.GetUserById(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UpdateUserRoles(int id, User newUserData, int[] selectedRoles)
        {
            try
            {
                // Get the existing user with their roles from the database
                var userToUpdate = await GetUserById(id);

                if (userToUpdate == null)
                {
                    return false;
                }

                // Update the user's name
                userToUpdate.Name = newUserData.Name;

                // Update roles
                if (userToUpdate.Roles != null && selectedRoles.Length > 0 && userToUpdate.Roles.Count > 0)
                {
                    var userRoles = userToUpdate.Roles!;
                    var removedRoles = userRoles.Where(r => !selectedRoles.Contains(r.Id)).ToList();
                    var newRolesIds = selectedRoles.Where(r => !userRoles.Any(ur => ur.Id == r)).ToList();

                    if (removedRoles != null && removedRoles.Count > 0)
                    {
                        userToUpdate.Roles.RemoveAll(r => removedRoles.Any(rr => rr.Id == r.Id));
                    }

                    // Add selected roles
                    if (newRolesIds != null && newRolesIds.Count > 0)
                    {
                        var rolesToAdd = await _rolesService.GetRolesByIds([.. newRolesIds]);

                        userToUpdate.Roles.AddRange(rolesToAdd);
                    }

                }
                else if (userToUpdate.Roles != null && selectedRoles.Length > 0 && userToUpdate.Roles.Count == 0)
                {
                    var rolesToAdd = await _rolesService.GetRolesByIds([.. selectedRoles]);

                    userToUpdate.Roles.AddRange(rolesToAdd);
                }
                else if (userToUpdate.Roles != null && selectedRoles.Length == 0)
                {
                    userToUpdate.Roles.Clear();
                }

                await UpdateUser(userToUpdate);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating user with id {Id}", id);
                if (!await UserExists(id))
                {
                    _logger.LogWarning("User with id {Id} no longer exists during update", id);
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<bool> UserExists(int id)
        {
            _logger.LogInformation("Checking if user exists with id: {id}", id);
            return await _context.Users.AnyAsync(e => e.Id == id);
        }
    }
}
