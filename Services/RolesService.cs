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

        public async Task<PaginatedList<Profil>> GetProfils(string searchString, int pageIndex)
        {
            _logger.LogInformation("Getting profils with search string: {SearchString} and page index: {PageIndex}", searchString, pageIndex);

            // NoTracking is recommended to make pages consistent when something changes during viewing it
            var profils = _context.Profils.AsNoTracking();
            // Apply search filer if provided

            // Apply searchString filter (case insensitive)
            if (!string.IsNullOrEmpty(searchString))
            {
                profils = profils.Where(u => u.profil!.ToUpper().Contains(searchString.ToUpper()));
            }

            // Add OrderBy to ensrue consistent ordering of results
            profils = profils.OrderBy(p => p.profil);

            return await PaginatedList<Profil>.CreateAsync(profils, pageIndex, _config.DefaultPageSize);
        }

        public async Task<Profil?> GetProfilById(int id)
        {
            _logger.LogInformation("Getting profil by id: {Id}", id);
            return await _context.Profils.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task CreateProfil(Profil profil)
        {
            _logger.LogInformation("Creating new profil with name: {ProfilName}", profil.profil);
            _context.Profils.Add(profil);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProfil(Profil profil)
        {
            _logger.LogInformation("Updating profil with id: {Id} and new name: {ProfilName}", profil.Id, profil.profil);
            _context.Profils.Update(profil);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProfil(Profil profil)
        {
            _logger.LogInformation("Deleting profil with id: {Id} and name: {ProfilName}", profil.Id, profil.profil);

            // Check if any users are assosiated with this profile
            if (profil.Users != null && profil.Users.Any())
            {
                _logger.LogInformation("Removing profil associations from {UserCount} users", profil.Users.Count);
                // Loop through each user and remove the profil association
                foreach (var user in profil.Users)
                {
                    user.Profils!.Remove(profil);
                }
            }

            // Remove profil
            _context.Profils.Remove(profil);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ProfilExists(int id)
        {
            return await _context.Profils.AnyAsync(p => p.Id == id);
        }

        public async Task<Profil?> GetProfilByIdWithUsers(int id)
        {
            _logger.LogInformation("Getting profil with users by id: {Id}", id);
            return await _context.Profils.Include(p => p.Users).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Profil>> GetAllProfils()
        {
            _logger.LogInformation("Getting all profils ordered by name");
            return await _context.Profils.OrderBy(p => p.profil).ToListAsync();
        }

        public async Task<List<Profil>> GetProfilsByIds(List<int> ids)
        {
            _logger.LogInformation("Getting profils by ids: {Ids}", string.Join(", ", ids));
            return await _context.Profils.Where(p => ids.Contains(p.Id)).ToListAsync();
        }
    }
}
