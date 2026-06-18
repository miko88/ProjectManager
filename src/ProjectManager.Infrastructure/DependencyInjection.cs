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
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        services.Configure<AuthOptions>(config.GetSection(AuthOptions.SectionName));

        services.AddSingleton<IProjectRepository, XmlProjectRepository>();
        services.AddSingleton<IUserAuthenticator, ConfigUserAuthenticator>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
