using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Application.Abstractions;
using ProjectManager.Infrastructure.Auth;
using ProjectManager.Infrastructure.Storage;

namespace ProjectManager.Infrastructure;

public static class DependencyInjection
{
    private const int MinSigningKeyBytes = 32;

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Storage options — reject a configured path whose file does not exist, so a mis-typed
        // directory OR file name fails fast at startup instead of silently looking like an empty
        // store (and creating a new file in the wrong place on the first write). The seed file is
        // always shipped/mounted, so requiring it to exist is correct for this app. Eager
        // validation is wired in the API host (ValidateOnStart).
        services.AddOptions<StorageOptions>()
            .Bind(config.GetSection(StorageOptions.SectionName))
            .Validate(o => File.Exists(Path.GetFullPath(o.ProjectsFilePath)),
                "Storage:ProjectsFilePath does not point to an existing file.");

        // Auth options — reject incomplete/invalid configuration up front rather than at the first
        // login request: present credentials, a valid Base64 password hash, a >= 32-byte signing
        // key, a positive token lifetime, and non-empty issuer/audience.
        services.AddOptions<AuthOptions>()
            .Bind(config.GetSection(AuthOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Username), "Auth:Username must be configured.")
            .Validate(o => IsValidPasswordHash(o.PasswordHash),
                "Auth:PasswordHash must be a valid Base64-encoded password hash.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && Encoding.UTF8.GetByteCount(o.SigningKey) >= MinSigningKeyBytes,
                "Auth:SigningKey must be configured and at least 32 bytes (HS256).")
            .Validate(o => o.TokenExpiryMinutes > 0, "Auth:TokenExpiryMinutes must be greater than zero.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Auth:Issuer must be configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Auth:Audience must be configured.");

        services.AddSingleton<IProjectRepository, XmlProjectRepository>();
        services.AddSingleton<IUserAuthenticator, ConfigUserAuthenticator>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }

    private static bool IsValidPasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var buffer = new byte[value.Length];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written < 1)
        {
            return false;
        }

        // ASP.NET Core Identity hash format: the first byte is the version marker.
        // v2 = 0x00 (marker + 16-byte salt + 20-byte subkey); v3 = 0x01 (marker + 12-byte header + salt + subkey).
        // This rejects arbitrary Base64 that merely decodes to the right length but isn't an Identity hash.
        return buffer[0] switch
        {
            0x00 => written >= 1 + 16 + 20,
            0x01 => written >= 1 + 12 + 16 + 16,
            _ => false
        };
    }
}
