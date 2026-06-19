using System.Net.Http.Headers;

namespace ProjectManager.Client.Auth;

public sealed class BearerTokenHandler(TokenStore store) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(store.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", store.Token);
        return base.SendAsync(request, ct);
    }
}
