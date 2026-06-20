using FluentAssertions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Features.Projects.ListProjects;
using ProjectManager.Domain;

namespace ProjectManager.Application.Tests.Features.Projects;

public class ListProjectsHandlerTests
{
    [Fact]
    public async Task ReturnsAllProjectsFromRepository()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([Project.Create("prj1", "A", "A", "C")]);

        var handler = new ListProjectsHandler(repo);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(1);
    }
}
