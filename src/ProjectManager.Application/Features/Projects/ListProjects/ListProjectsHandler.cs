using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Domain;

namespace ProjectManager.Application.Features.Projects.ListProjects;

public sealed class ListProjectsHandler(IProjectRepository repository)
{
    public async Task<Result<IReadOnlyList<Project>>> HandleAsync(CancellationToken ct = default)
    {
        var projects = await repository.GetAllAsync(ct);
        return Result<IReadOnlyList<Project>>.Success(projects);
    }
}
