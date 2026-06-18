namespace ProjectManager.Application.Abstractions;

public sealed record TokenResult(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    TokenResult CreateToken(AuthenticatedUser user);
}
