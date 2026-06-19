using FluentValidation;
using Microsoft.Extensions.Logging;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IUserAuthenticator authenticator,
    ITokenService tokenService,
    IValidator<LoginCommand> validator,
    ILogger<LoginHandler> logger)
{
    public async Task<Result<TokenResult>> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return Result<TokenResult>.Invalid(validation.ToErrorDictionary());
        }

        var auth = await authenticator.AuthenticateAsync(command.Username, command.Password, ct);
        if (!auth.IsSuccess)
        {
            // Username only — never the password — so failed-login auditing stays safe.
            logger.LogWarning("Failed login attempt for user {User}", command.Username);
            return Result<TokenResult>.Unauthorized(ResultMessages.InvalidCredentials);
        }

        var token = tokenService.CreateToken(auth.Value!);
        logger.LogInformation("User {User} logged in", command.Username);
        return Result<TokenResult>.Success(token);
    }
}
