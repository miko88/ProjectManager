using ProjectManager.Api.Common;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Auth.Login;
using ProjectManager.Contracts;

namespace ProjectManager.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (LoginRequest req, LoginHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new LoginCommand(req.Username, req.Password), ct);
            return result.IsSuccess
                ? Results.Ok(new LoginResponse(result.Value!.Token, result.Value.ExpiresAt))
                : result.ToProblem();
        })
        .AllowAnonymous();
    }
}
