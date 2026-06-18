using ProjectManager.Api.Common;
using ProjectManager.Application.Features.Projects.CreateProject;
using ProjectManager.Application.Features.Projects.DeleteProject;
using ProjectManager.Application.Features.Projects.ListProjects;
using ProjectManager.Application.Features.Projects.UpdateProject;
using ProjectManager.Contracts;
using ProjectManager.Domain;

namespace ProjectManager.Api.Endpoints;

public static class ProjectsEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapGet("/", async (ListProjectsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            return Results.Ok(result.Value!.Select(ToDto));
        });

        group.MapGet("/{id}", async (string id, ListProjectsHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ct);
            var project = result.Value!.FirstOrDefault(p => p.Id == id);
            return project is null
                ? Results.Problem($"Project '{id}' was not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(ToDto(project));
        });

        group.MapPost("/", async (CreateProjectRequest req, CreateProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new CreateProjectCommand(req.Name, req.Abbreviation, req.Customer), ct);
            return result.IsSuccess
                ? Results.Created($"/api/projects/{result.Value!.Id}", ToDto(result.Value))
                : result.ToProblem();
        });

        group.MapPut("/{id}", async (string id, UpdateProjectRequest req, UpdateProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new UpdateProjectCommand(id, req.Name, req.Abbreviation, req.Customer), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        group.MapDelete("/{id}", async (string id, DeleteProjectHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(id, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });
    }

    private static ProjectDto ToDto(Project p) => new(p.Id, p.Name, p.Abbreviation, p.Customer);
}
