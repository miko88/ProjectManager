using Microsoft.Extensions.Logging;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Projects.DeleteProject;

public sealed class DeleteProjectHandler(
    IProjectRepository repository,
    ILogger<DeleteProjectHandler> logger)
{
    public async Task<Result> HandleAsync(string id, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result.NotFound($"Project '{id}' was not found.");

        await repository.DeleteAsync(id, ct);

        logger.LogInformation("Project {ProjectId} deleted", id);
        return Result.Success();
    }
}
