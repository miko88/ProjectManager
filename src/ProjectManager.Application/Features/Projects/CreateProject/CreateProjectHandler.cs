using FluentValidation;
using Microsoft.Extensions.Logging;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Domain;

namespace ProjectManager.Application.Features.Projects.CreateProject;

public sealed class CreateProjectHandler(
    IProjectRepository repository,
    IValidator<CreateProjectCommand> validator,
    ILogger<CreateProjectHandler> logger)
{
    public async Task<Result<Project>> HandleAsync(CreateProjectCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return Result<Project>.Invalid(validation.ToErrorDictionary());
        }

        var existing = await repository.GetAllAsync(ct);
        if (existing.Any(p => string.Equals(p.Abbreviation, command.Abbreviation.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result<Project>.Conflict(ResultMessages.DuplicateAbbreviation(command.Abbreviation));
        }

        // Id generation + persistence happen atomically inside the repository's write lock,
        // so concurrent creates can never collide on an id.
        var project = await repository.AddAsync(
            id => Project.Create(id, command.Name, command.Abbreviation, command.Customer), ct);

        logger.LogInformation("Project {ProjectId} created (abbreviation {Abbreviation})", project.Id, project.Abbreviation);
        return Result<Project>.Success(project);
    }
}
