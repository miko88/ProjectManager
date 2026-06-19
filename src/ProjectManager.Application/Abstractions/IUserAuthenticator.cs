using ProjectManager.Application.Common;

namespace ProjectManager.Application.Abstractions;

public sealed record AuthenticatedUser(string Username);

public interface IUserAuthenticator
{
    Task<Result<AuthenticatedUser>> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}
