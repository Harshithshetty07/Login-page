using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AuthApi.Models;
using AuthApi.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens; 


namespace AuthApi.Services;

public interface ITokenService
{
    string CreateAccessToken(User user);
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;

        // HS256 needs a key of at least 256 bits (32 bytes)
        if (Encoding.UTF8.GetByteCount(_settings.Key) < 32)
            throw new InvalidOperationException(
                "Jwt:Key is missing or too short. Set it with dotnet user-secrets.");
    }

    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("name", user.FullName),
                new Claim("role", user.Role),
                new Claim("jti", Guid.NewGuid().ToString())
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}