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

        // The repository creates the project atomically — id generation and abbreviation
        // uniqueness are enforced by the adapter and surfaced as Conflict on collision.
        var result = await repository.CreateAsync(
            new ProjectDraft(command.Name, command.Abbreviation, command.Customer), ct);

        if (result.IsSuccess)
        {
            logger.LogInformation("Project {ProjectId} created (abbreviation {Abbreviation})", result.Value!.Id, result.Value.Abbreviation);
        }

        return result;
    }
}
