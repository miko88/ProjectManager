using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Common;
using ProjectManager.Infrastructure.Auth;
using Xunit;

namespace ProjectManager.Infrastructure.Tests;

public class ConfigUserAuthenticatorTests
{
    private static (ConfigUserAuthenticator auth, string password) Build()
    {
        var hasher = new PasswordHasher<string>();
        var hash = hasher.HashPassword("admin", "Secret123!");
        var options = Options.Create(new AuthOptions { Username = "admin", PasswordHash = hash });
        return (new ConfigUserAuthenticator(options), "Secret123!");
    }

    [Fact]
    public async Task CorrectCredentials_ReturnsSuccess()
    {
        var (auth, password) = Build();
        var result = await auth.AuthenticateAsync("admin", password);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("admin");
    }

    [Fact]
    public async Task WrongPassword_ReturnsUnauthorized()
    {
        var (auth, _) = Build();
        var result = await auth.AuthenticateAsync("admin", "wrong");
        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task UnknownUser_ReturnsUnauthorized()
    {
        var (auth, password) = Build();
        var result = await auth.AuthenticateAsync("intruder", password);
        result.Status.Should().Be(ResultStatus.Unauthorized);
    }
}
