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
            return Result.Invalid(validation.ToErrorDictionary());

        var project = await repository.GetByIdAsync(command.Id, ct);
        if (project is null)
            return Result.NotFound($"Project '{command.Id}' was not found.");

        var others = await repository.GetAllAsync(ct);
        if (others.Any(p => p.Id != command.Id &&
                            string.Equals(p.Abbreviation, command.Abbreviation.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Result.Conflict($"A project with abbreviation '{command.Abbreviation}' already exists.");

        project.Update(command.Name, command.Abbreviation, command.Customer);
        await repository.UpdateAsync(project, ct);

        logger.LogInformation("Project {ProjectId} updated", project.Id);
        return Result.Success();
    }
}
