using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Infrastructure.Auth;

/// <summary>
/// Mock user store: a single user read from configuration, password verified against a
/// stored PBKDF2 hash (never plaintext). Behind <see cref="IUserAuthenticator"/> so it can
/// be swapped for a real identity provider without touching the application layer.
/// </summary>
public sealed class ConfigUserAuthenticator(IOptions<AuthOptions> options) : IUserAuthenticator
{
    private readonly AuthOptions _options = options.Value;
    private readonly PasswordHasher<string> _hasher = new();

    public Task<Result<AuthenticatedUser>> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        if (!string.Equals(username, _options.Username, StringComparison.Ordinal))
        {
            return Task.FromResult(Result<AuthenticatedUser>.Unauthorized("Invalid credentials."));
        }

        var verify = _hasher.VerifyHashedPassword(username, _options.PasswordHash, password);
        if (verify == PasswordVerificationResult.Failed)
        {
            return Task.FromResult(Result<AuthenticatedUser>.Unauthorized("Invalid credentials."));
        }

        return Task.FromResult(Result<AuthenticatedUser>.Success(new AuthenticatedUser(username)));
    }
}
