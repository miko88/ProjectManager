using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Domain;

namespace ProjectManager.Application.Features.Projects.GetProject;

public sealed class GetProjectHandler(IProjectRepository repository)
{
    public async Task<Result<Project>> HandleAsync(string id, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(id, ct);
        return project is null
            ? Result<Project>.NotFound($"Project '{id}' was not found.")
            : Result<Project>.Success(project);
    }
}
