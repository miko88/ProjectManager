using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProjectManager.Client.Auth;

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private string? _token;

    public string? Token => _token;

    public void SetToken(string token)
    {
        _token = token;
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public void Clear()
    {
        _token = null;
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(BuildState());

    private AuthenticationState BuildState()
    {
        if (string.IsNullOrEmpty(_token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_token);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
