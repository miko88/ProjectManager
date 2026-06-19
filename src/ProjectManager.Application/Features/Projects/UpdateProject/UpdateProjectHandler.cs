using FluentValidation;
using Microsoft.Extensions.Logging;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;

namespace ProjectManager.Application.Features.Projects.UpdateProject;

public sealed class UpdateProjectHandler(
    IProjectRepository repository,
    IValidator<UpdateProjectCommand> validator,
    ILogger<UpdateProjectHandler> logger)
{
    public async Task<Result> HandleAsync(UpdateProjectCommand command, CancellationToken ct = default)
    {
        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return Result.Invalid(validation.ToErrorDictionary());
        }

        // The repository updates atomically — existence and abbreviation uniqueness are enforced
        // by the adapter and surfaced as NotFound / Conflict.
        var result = await repository.UpdateAsync(
            command.Id, new ProjectDraft(command.Name, command.Abbreviation, command.Customer), ct);

        if (result.IsSuccess)
        {
            logger.LogInformation("Project {ProjectId} updated", command.Id);
        }

        return result;
    }
}
