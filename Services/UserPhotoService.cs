using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using Microsoft.Extensions.Logging;

// This service retrieves a user's photo from Active Directory and returns it as a Base64 string.
public class UserPhotoService
{
    private readonly ILogger<UserPhotoService> _logger;

    public UserPhotoService(ILogger<UserPhotoService> logger)
    {
        _logger = logger;
    }

    public string? GetUserPhotoBase64(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            // _logger.LogWarning("GetUserPhotoBase64 called with empty username.");
            return null;
        }

        try
        {
            // _logger.LogDebug("Attempting to retrieve photo for user: {Username}", username);

            using (var context = new PrincipalContext(ContextType.Domain))
            using (var user = UserPrincipal.FindByIdentity(context, username))
            {
                if (user != null)
                {
                    var de = user.GetUnderlyingObject() as DirectoryEntry;
                    if (de != null && de.Properties["thumbnailPhoto"].Value is byte[] photoBytes)
                    {
                        _logger.LogDebug("Photo found for user: {Username}", username);
                        return Convert.ToBase64String(photoBytes);
                    }
                    else
                    {
                        // _logger.LogDebug("No photo found for user: {Username}", username);
                    }
                }
                else
                {
                    // _logger.LogDebug("UserPrincipal not found for username: {Username}", username);
                }
            }
        }
        catch
        {
            // _logger.LogDebug("Error retrieving photo for user: {Username}", username);
        }

        return null;
    }
}