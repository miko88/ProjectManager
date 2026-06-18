using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Projects.DeleteProject;

public sealed class DeleteProjectHandler(IProjectRepository repository)
{
    public async Task<Result> HandleAsync(string id, CancellationToken ct = default)
    {
        var project = await repository.GetByIdAsync(id, ct);
        if (project is null)
            return Result.NotFound($"Project '{id}' was not found.");

        await repository.DeleteAsync(id, ct);
        return Result.Success();
    }
}
