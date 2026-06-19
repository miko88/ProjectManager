using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.GetProject;
using ProjectManager.Domain;

namespace ProjectManager.Application.Tests.Features.Projects;

public class GetProjectHandlerTests
{
    [Fact]
    public async Task ExistingProject_IsReturned()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("prj1", Arg.Any<CancellationToken>())
            .Returns(Project.Create("prj1", "A", "AB", "C"));

        var result = await new GetProjectHandler(repo).HandleAsync("prj1");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("prj1");
    }

    [Fact]
    public async Task MissingProject_ReturnsNotFound()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetByIdAsync("nope", Arg.Any<CancellationToken>()).Returns((Project?)null);

        var result = await new GetProjectHandler(repo).HandleAsync("nope");

        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
