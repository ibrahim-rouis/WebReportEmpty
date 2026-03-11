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


        public UsersService(WebReportDBContext context, IOptions<WebReportConfig> webReportConfig, ILogger<UsersService> logger)
        {
            _context = context;
            _config = webReportConfig.Value;
            _logger = logger;
        }

        public async Task<PaginatedList<User>> GetUsers(string searchString, int pageIndex, int? profilIdFilter)
        {
            _logger.LogInformation("Getting users with searchString: {searchString}, pageIndex: {pageIndex}, profilIdFilter: {profilIdFilter}", searchString, pageIndex, profilIdFilter);

            // NoTracking is recommended to make pages consistent when something changes during viewing it
            var users = _context.Users.Include(u => u.Profils).AsNoTracking();

            // Apply search filer if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u => u.Nom!.ToUpper().Contains(searchString.ToUpper()));
            }
            if (profilIdFilter.HasValue)
            {
                users = users.Where(u => u.Profils!.Any(p => p.Id == profilIdFilter));

            }

            // Add OrderBy to ensure consistent ordering of results
            users = users.OrderBy(u => u.Id);

            return await PaginatedList<User>.CreateAsync(users, pageIndex, _config.DefaultPageSize);
        }

        public async Task<User?> GetUserById(int id)
        {
            _logger.LogInformation("Getting user by id: {id}", id);
            return await _context.Users.Include(u => u.Profils).FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task CreateUser(User user)
        {
            _logger.LogInformation("Creating user with name: {name}", user.Nom);
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

        public async Task<bool> userExists(int id)
        {
            _logger.LogInformation("Checking if user exists with id: {id}", id);
            return await _context.Users.AnyAsync(e => e.Id == id);
        }
    }
}
