using FluentValidation;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Auth.Login;

public sealed class LoginHandler(
    IUserAuthenticator authenticator,
    ITokenService tokenService,
    IValidator<LoginCommand> validator)
{
    public async Task<Result<TokenResult>> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<TokenResult>.Invalid(validation.ToErrorDictionary());

        var auth = await authenticator.AuthenticateAsync(command.Username, command.Password, ct);
        if (!auth.IsSuccess)
            return Result<TokenResult>.Unauthorized("Invalid username or password.");

        var token = tokenService.CreateToken(auth.Value!);
        return Result<TokenResult>.Success(token);
    }
}
