using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProjectManager.Client.Auth;

public sealed class JwtAuthenticationStateProvider(TokenStore store) : AuthenticationStateProvider
{
    public void SetToken(string token)
    {
        store.Token = token;
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public void Clear()
    {
        store.Token = null;
        NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(BuildState());

    private AuthenticationState BuildState()
    {
        if (string.IsNullOrEmpty(store.Token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(store.Token);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
