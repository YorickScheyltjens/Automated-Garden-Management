using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GardenSystem.Application.Abstractions;
using GardenSystem.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GardenSystem.Infrastructure.Security;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> jwtOptions) : IJwtTokenGenerator
{
    public string GenerateAccessToken(Guid userId, string email)
    {
        var options = jwtOptions.Value;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(options.AccessTokenLifetimeMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
