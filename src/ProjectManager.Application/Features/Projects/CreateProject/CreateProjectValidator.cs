using FluentValidation;

namespace ProjectManager.Application.Features.Projects.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Abbreviation).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Customer).NotEmpty().MaximumLength(200);
    }
}
