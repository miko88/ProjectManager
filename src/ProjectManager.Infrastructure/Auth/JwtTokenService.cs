using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjectManager.Application.Abstractions;

namespace ProjectManager.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
    private const int MinSigningKeyBytes = 32;

    private readonly AuthOptions _options;

    public JwtTokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrEmpty(_options.SigningKey) ||
            Encoding.UTF8.GetByteCount(_options.SigningKey) < MinSigningKeyBytes)
        {
            throw new ArgumentException(
                $"HS256 requires a signing key of at least {MinSigningKeyBytes} bytes (256 bits). " +
                "Configure a longer 'SigningKey' value.",
                nameof(options));
        }
    }

    public TokenResult CreateToken(AuthenticatedUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.TokenExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResult(encoded, expires);
    }
}
