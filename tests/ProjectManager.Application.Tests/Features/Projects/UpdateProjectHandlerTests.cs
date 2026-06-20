using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.UpdateProject;

namespace ProjectManager.Application.Tests.Features.Projects;

public class UpdateProjectHandlerTests
{
    private static UpdateProjectHandler Build(IProjectRepository repo) =>
        new(repo, new UpdateProjectValidator(), NullLogger<UpdateProjectHandler>.Instance);

    [Fact]
    public async Task ValidCommand_DelegatesToRepository_AndSucceeds()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.UpdateAsync("prj1", Arg.Any<ProjectDraft>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("prj1", "New", "NEW", "NewC"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateAsync("prj1",
            Arg.Is<ProjectDraft>(d => d.Name == "New" && d.Abbreviation == "NEW" && d.Customer == "NewC"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingProject_PropagatesNotFound()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.UpdateAsync("nope", Arg.Any<ProjectDraft>(), Arg.Any<CancellationToken>())
            .Returns(Result.NotFound("missing"));

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("nope", "New", "NEW", "NewC"), TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task DuplicateAbbreviation_PropagatesConflict()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.UpdateAsync(Arg.Any<string>(), Arg.Any<ProjectDraft>(), Arg.Any<CancellationToken>())
            .Returns(Result.Conflict("duplicate"));

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("prj1", "New", "DUP", "NewC"), TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public async Task BlankName_ReturnsInvalid_AndDoesNotCallRepository()
    {
        var repo = Substitute.For<IProjectRepository>();

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("prj1", "", "NEW", "NewC"), TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResultStatus.Invalid);
        await repo.DidNotReceive().UpdateAsync(Arg.Any<string>(), Arg.Any<ProjectDraft>(), Arg.Any<CancellationToken>());
    }
}
