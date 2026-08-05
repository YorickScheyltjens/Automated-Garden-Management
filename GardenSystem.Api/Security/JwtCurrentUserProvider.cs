using System.IdentityModel.Tokens.Jwt;
using GardenSystem.Application.Abstractions;

namespace GardenSystem.Api.Security;

public sealed class JwtCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public Guid GetCurrentUserId()
    {
        var subClaim = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub);

        if (subClaim is null || !Guid.TryParse(subClaim.Value, out var userId))
        {
            throw new InvalidOperationException("No authenticated user found on the current request.");
        }

        return userId;
    }
}
