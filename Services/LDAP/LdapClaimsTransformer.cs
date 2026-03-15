using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Principal;

namespace WebReport.Services.LDAP
{
    public class LdapClaimsTransformer : IClaimsTransformation
    {
        private readonly LdapService _ldapService;

        public LdapClaimsTransformer(LdapService ldapService)
        {
            _ldapService = ldapService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Clone the identity so we don't modify the original one
            var clone = principal.Clone();
            var newIdentity = (ClaimsIdentity)clone.Identity!;

            if (!newIdentity.IsAuthenticated) return principal;

            // Extract the username (stripping domain if it's Windows Auth)
            string username = newIdentity.Name!.Split('\\').Last();

            // Fetch groups from LDAP
            var ldapGroups = _ldapService.GetUserGroups(username);

            // Map LDAP groups to Application Roles
            foreach (var group in ldapGroups)
            {
                // You can add the group exactly as it is in LDAP
                newIdentity.AddClaim(new Claim(ClaimTypes.Role, group));

                // Or map specific LDAP groups to friendly local names
                /*
                if (group == "LEONI_IT_ADMINS") 
                    newIdentity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                */
            }

            // 1. Fetch the photo bytes from your LdapService
            // Using the GetUserPhoto method we discussed earlier
            byte[]? photoBytes = _ldapService.GetUserPhoto(username);

            if (photoBytes != null && photoBytes.Length > 0)
            {
                // 2. Convert to Base64
                string base64Photo = Convert.ToBase64String(photoBytes);

                // 3. Add as a Claim
                // Note: Claims are strings, so Base64 is perfect for this.
                newIdentity.AddClaim(new Claim("UserPhoto", base64Photo));
            }

            return clone;
        }
    }
}
