using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.UpdateProject;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Projects;

public class UpdateProjectHandlerTests
{
    private static UpdateProjectHandler Build(IProjectRepository repo) =>
        new(repo, new UpdateProjectValidator());

    [Fact]
    public async Task ExistingProject_IsUpdatedAndPersisted()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("prj1", Arg.Any<CancellationToken>())
            .Returns(Project.Create("prj1", "Old", "OLD", "OldC"));
        repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Project.Create("prj1", "Old", "OLD", "OldC") });

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("prj1", "New", "NEW", "NewC"));

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).UpdateAsync(Arg.Is<Project>(p => p.Name == "New"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingProject_ReturnsNotFound()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("nope", "New", "NEW", "NewC"));

        result.Status.Should().Be(ResultStatus.NotFound);
        await repo.DidNotReceive().UpdateAsync(Arg.Any<ProjectManager.Domain.Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlankName_ReturnsInvalid()
    {
        var repo = Substitute.For<IProjectRepository>();
        var result = await Build(repo).HandleAsync(new UpdateProjectCommand("prj1", "", "NEW", "NewC"));
        result.Status.Should().Be(ResultStatus.Invalid);
    }
}
