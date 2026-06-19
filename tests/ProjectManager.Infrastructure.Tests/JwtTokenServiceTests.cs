using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Abstractions;
using ProjectManager.Infrastructure.Auth;
using Xunit;

namespace ProjectManager.Infrastructure.Tests;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_ProducesValidJwt_WithExpectedClaims()
    {
        var options = Options.Create(new AuthOptions
        {
            Issuer = "ProjectManager",
            Audience = "ProjectManagerClient",
            TokenExpiryMinutes = 30,
            SigningKey = "this-is-a-long-enough-test-signing-key-0123456789"
        });
        var service = new JwtTokenService(options);

        var result = service.CreateToken(new AuthenticatedUser("admin"));

        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Issuer.Should().Be("ProjectManager");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "admin");
    }

    [Fact]
    public void Constructor_WithTooShortSigningKey_ThrowsArgumentException()
    {
        var options = Options.Create(new AuthOptions
        {
            Issuer = "ProjectManager",
            Audience = "ProjectManagerClient",
            TokenExpiryMinutes = 30,
            SigningKey = "short"
        });

        var act = () => new JwtTokenService(options);

        act.Should().Throw<ArgumentException>();
    }
}
