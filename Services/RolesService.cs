using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebReport.Configuration;
using WebReport.Models;
using WebReport.Models.Common;
using WebReport.Models.Entities;

namespace WebReport.Services
{
    public class RolesService
    {
        private readonly WebReportDBContext _context;
        private readonly WebReportConfig _config;
        private readonly ILogger<RolesService> _logger;
        public RolesService(WebReportDBContext context, IOptions<WebReportConfig> webReportConfig, ILogger<RolesService> logger)
        {
            _context = context;
            _config = webReportConfig.Value;
            _logger = logger;
        }

        public async Task<PaginatedList<Role>> GetRoles(string searchString, int pageIndex)
        {
            _logger.LogInformation("Getting roles with search string: {SearchString} and page index: {PageIndex}", searchString, pageIndex);

            // NoTracking is recommended to make pages consistent when something changes during viewing it
            var roles = _context.Roles.AsNoTracking();
            // Apply search filer if provided

            // Apply searchString filter (case insensitive)
            if (!string.IsNullOrEmpty(searchString))
            {
                roles = roles.Where(u => u.Name!.ToUpper().Contains(searchString.ToUpper()));
            }

            // Add OrderBy to ensrue consistent ordering of results
            roles = roles.OrderBy(p => p.Name);

            return await PaginatedList<Role>.CreateAsync(roles, pageIndex, _config.DefaultPageSize);
        }

        public async Task<Role?> GetRoleById(int id)
        {
            _logger.LogInformation("Getting role by id: {Id}", id);
            return await _context.Roles.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task CreateRole(Role role)
        {
            _logger.LogInformation("Creating new role with name: {RoleName}", role.Name);
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRole(Role role)
        {
            _logger.LogInformation("Updating role with id: {Id} and new name: {RoleName}", role.Id, role.Name);
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRole(Role role)
        {
            _logger.LogInformation("Deleting role with id: {Id} and name: {RoleName}", role.Id, role.Name);

            // Check if any users are assosiated with this role
            if (role.Users != null && role.Users.Any())
            {
                _logger.LogInformation("Removing role associations from {UserCount} users", role.Users.Count);
                // Loop through each user and remove the role association
                foreach (var user in role.Users)
                {
                    user.Roles!.Remove(role);
                }
            }

            // Remove role from database
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RoleExists(int id)
        {
            return await _context.Roles.AnyAsync(p => p.Id == id);
        }

        public async Task<Role?> GetRoleByIdWithUsers(int id)
        {
            _logger.LogInformation("Getting role with users by id: {Id}", id);
            return await _context.Roles.Include(p => p.Users).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Role>> GetAllRoles()
        {
            _logger.LogInformation("Getting all roles ordered by name");
            return await _context.Roles.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<List<Role>> GetRolesByIds(List<int> ids)
        {
            _logger.LogInformation("Getting roles by ids: {Ids}", string.Join(", ", ids));
            return await _context.Roles.Where(p => ids.Contains(p.Id)).ToListAsync();
        }
    }
}
