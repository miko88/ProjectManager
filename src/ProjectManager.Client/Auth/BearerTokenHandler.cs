using System.Net.Http.Headers;

namespace ProjectManager.Client.Auth;

public sealed class BearerTokenHandler(JwtAuthenticationStateProvider authState) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(authState.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authState.Token);
        return base.SendAsync(request, ct);
    }
}
