using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace WebReport.Services.LDAP
{
    public class LdapClaimsTransformer : IClaimsTransformation
    {
        private readonly WindowsUserService _windowsUserService;
        private readonly LdapService _ldapService;
        private readonly IMemoryCache _cache;
        private readonly IHostEnvironment _hostEnvironment;

        public LdapClaimsTransformer(
            WindowsUserService windowsUserService,
            IMemoryCache cache,
            LdapService ldapService,
            IHostEnvironment hostEnvironment
        )
        {
            _windowsUserService = windowsUserService;
            _cache = cache;
            _ldapService = ldapService;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var clone = principal.Clone();
            var newIdentity = (ClaimsIdentity)clone.Identity!;

            if (!newIdentity.IsAuthenticated || string.IsNullOrEmpty(newIdentity.Name))
                return principal;

            string username = newIdentity.Name; // Typically looks like "DOMAIN\username"

            // Strip the domain prefix if your database just stores "username"
            if (username.Contains("\\"))
            {
                username = username.Split('\\')[1];
            }

            string cacheKey = $"UserRoles_{username}";

            // Check cache to avoid hitting the database on every HTTP request
            if (!_cache.TryGetValue(cacheKey, out List<string>? cachedRoleNames))
            {
                // 1. Try to get user from DB
                var dbUser = await _windowsUserService.GetWindowsUserByName(username);
                var adGroups = _ldapService.GetUserGroups(username);

                // 2. If user doesn't exist in DB, you might want to create them here!
                if (dbUser == null)
                {
                    // create user with roles based on AD groups
                    dbUser = await _windowsUserService.SaveWindowsUser(username, adGroups);
                }

                cachedRoleNames = adGroups;

                // Store in memory cache for 15 minutes
                _cache.Set(cacheKey, cachedRoleNames, TimeSpan.FromMinutes(15));
            }

            // 3. Inject Database roles into the current Identity
            if (cachedRoleNames != null)
            {
                foreach (var roleName in cachedRoleNames)
                {
                    if (!newIdentity.HasClaim(ClaimTypes.Role, roleName))
                    {
                        newIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
            }

            return clone;
        }
    }
}

