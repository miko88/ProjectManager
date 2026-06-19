using FluentValidation;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Domain;

namespace ProjectManager.Application.Features.Projects.CreateProject;

public sealed class CreateProjectHandler(
    IProjectRepository repository,
    IValidator<CreateProjectCommand> validator)
{
    public async Task<Result<Project>> HandleAsync(CreateProjectCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return Result<Project>.Invalid(validation.ToErrorDictionary());

        var existing = await repository.GetAllAsync(ct);
        if (existing.Any(p => string.Equals(p.Abbreviation, command.Abbreviation.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result<Project>.Conflict($"A project with abbreviation '{command.Abbreviation}' already exists.");

        var id = await repository.NextIdAsync(ct);
        var project = Project.Create(id, command.Name, command.Abbreviation, command.Customer);
        await repository.AddAsync(project, ct);

        return Result<Project>.Success(project);
    }
}
