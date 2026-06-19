using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectManager.Application.Abstractions;
using ProjectManager.Infrastructure.Auth;
using ProjectManager.Infrastructure.Storage;

namespace ProjectManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Storage options — reject a configured directory that does not exist, so a mis-typed
        // path fails fast instead of silently looking like an empty store (and writing to the
        // wrong place on the first save). A missing file inside a valid directory is still fine
        // (first run / store gets created). Eager validation is wired in the API host (ValidateOnStart).
        services.AddOptions<StorageOptions>()
            .Bind(config.GetSection(StorageOptions.SectionName))
            .Validate(o =>
            {
                var dir = Path.GetDirectoryName(Path.GetFullPath(o.ProjectsFilePath));
                return !string.IsNullOrEmpty(dir) && Directory.Exists(dir);
            }, "Storage:ProjectsFilePath points to a directory that does not exist.");

        // Auth options — reject missing credentials or a missing/too-short signing key up front,
        // rather than failing on the first login request.
        services.AddOptions<AuthOptions>()
            .Bind(config.GetSection(AuthOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Username), "Auth:Username must be configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.PasswordHash), "Auth:PasswordHash must be configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
                "Auth:SigningKey must be configured and at least 32 bytes (HS256).");

        services.AddSingleton<IProjectRepository, XmlProjectRepository>();
        services.AddSingleton<IUserAuthenticator, ConfigUserAuthenticator>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
