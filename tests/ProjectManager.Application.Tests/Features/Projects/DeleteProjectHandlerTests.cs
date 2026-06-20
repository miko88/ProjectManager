using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.DeleteProject;
using ProjectManager.Domain;

namespace ProjectManager.Application.Tests.Features.Projects;

public class DeleteProjectHandlerTests
{
    private static DeleteProjectHandler Build(IProjectRepository repo) =>
        new(repo, NullLogger<DeleteProjectHandler>.Instance);

    [Fact]
    public async Task ExistingProject_IsDeleted()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("prj1", Arg.Any<CancellationToken>())
            .Returns(Project.Create("prj1", "A", "A", "C"));

        var result = await Build(repo).HandleAsync("prj1", TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).DeleteAsync("prj1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingProject_ReturnsNotFound()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await Build(repo).HandleAsync("nope", TestContext.Current.CancellationToken);

        result.Status.Should().Be(ResultStatus.NotFound);
        await repo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
