namespace ProjectManager.Client.Auth;

/// <summary>
/// Holds the current JWT for the app session.
/// Registered as a <b>singleton</b> on purpose: <see cref="System.Net.Http.IHttpClientFactory"/>
/// resolves message handlers (and their dependencies) from a separate DI scope than the Blazor
/// UI components. A scoped token holder would therefore give the <see cref="BearerTokenHandler"/>
/// a different instance than the one the login page wrote to, and no token would be attached.
/// A singleton store is shared across both scopes, so the handler always sees the current token.
/// </summary>
public sealed class TokenStore
{
    public string? Token { get; set; }
}
