using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.DeleteProject;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Projects;

public class DeleteProjectHandlerTests
{
    [Fact]
    public async Task ExistingProject_IsDeleted()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("prj1", Arg.Any<CancellationToken>())
            .Returns(Project.Create("prj1", "A", "A", "C"));

        var result = await new DeleteProjectHandler(repo).HandleAsync("prj1");

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).DeleteAsync("prj1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingProject_ReturnsNotFound()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await new DeleteProjectHandler(repo).HandleAsync("nope");

        result.Status.Should().Be(ResultStatus.NotFound);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
