using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.CreateProject;
using ProjectManager.Domain;

namespace ProjectManager.Application.Tests.Features.Projects;

public class CreateProjectHandlerTests
{
    private static CreateProjectHandler Build(IProjectRepository repo) =>
        new(repo, new CreateProjectValidator(), NullLogger<CreateProjectHandler>.Instance);

    [Fact]
    public async Task ValidCommand_DelegatesToRepository_AndReturnsCreated()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.CreateAsync(Arg.Any<ProjectDraft>(), Arg.Any<CancellationToken>())
            .Returns(Result<Project>.Success(Project.Create("prj6", "Name", "ABBR", "Cust")));

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("Name", "ABBR", "Cust"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("prj6");
        await repo.Received(1).CreateAsync(
            Arg.Is<ProjectDraft>(d => d.Name == "Name" && d.Abbreviation == "ABBR" && d.Customer == "Cust"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlankName_ReturnsInvalid_AndDoesNotPersist()
    {
        var repo = Substitute.For<IProjectRepository>();

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("", "ABBR", "Cust"), TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainKey(nameof(CreateProjectCommand.Name));
        await repo.DidNotReceive().CreateAsync(Arg.Any<ProjectDraft>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateAbbreviation_PropagatesConflictFromRepository()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.CreateAsync(Arg.Any<ProjectDraft>(), Arg.Any<CancellationToken>())
            .Returns(Result<Project>.Conflict("duplicate"));

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("Name", "ABBR", "Cust"), TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResultStatus.Conflict);
    }
}
