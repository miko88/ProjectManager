using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Auth.Login;

namespace ProjectManager.Application.Tests.Features.Auth;

public class LoginHandlerTests
{
    private static LoginHandler Build(IUserAuthenticator auth, ITokenService tokens) =>
        new(auth, tokens, new LoginValidator(), NullLogger<LoginHandler>.Instance);

    [Fact]
    public async Task ValidCredentials_ReturnsToken()
    {
        var auth = Substitute.For<IUserAuthenticator>();
        auth.AuthenticateAsync("admin", "pw", Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Success(new AuthenticatedUser("admin")));
        var tokens = Substitute.For<ITokenService>();
        tokens.CreateToken(Arg.Any<AuthenticatedUser>())
            .Returns(new TokenResult("jwt-123", DateTimeOffset.UtcNow.AddHours(1)));

        var result = await Build(auth, tokens).HandleAsync(new LoginCommand("admin", "pw"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("jwt-123");
    }

    [Fact]
    public async Task BadCredentials_ReturnsUnauthorized_AndNoToken()
    {
        var auth = Substitute.For<IUserAuthenticator>();
        auth.AuthenticateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Unauthorized("bad"));
        var tokens = Substitute.For<ITokenService>();

        var result = await Build(auth, tokens).HandleAsync(new LoginCommand("admin", "wrong"));

        result.Status.Should().Be(ResultStatus.Unauthorized);
        tokens.DidNotReceive().CreateToken(Arg.Any<AuthenticatedUser>());
    }

    [Fact]
    public async Task BlankUsername_ReturnsInvalid()
    {
        var result = await Build(Substitute.For<IUserAuthenticator>(), Substitute.For<ITokenService>())
            .HandleAsync(new LoginCommand("", "pw"));
        result.Status.Should().Be(ResultStatus.Invalid);
    }
}
