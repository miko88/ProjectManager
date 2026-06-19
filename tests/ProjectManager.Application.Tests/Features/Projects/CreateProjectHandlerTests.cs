using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Application.Features.Projects.CreateProject;
using ProjectManager.Domain;
using Xunit;

namespace ProjectManager.Application.Tests.Features.Projects;

public class CreateProjectHandlerTests
{
    private static CreateProjectHandler Build(IProjectRepository repo) =>
        new(repo, new CreateProjectValidator(), NullLogger<CreateProjectHandler>.Instance);

    [Fact]
    public async Task ValidCommand_AddsProjectWithGeneratedId()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Project>());
        // The repository assigns the id atomically; emulate it by invoking the factory with "prj6".
        repo.AddAsync(Arg.Any<Func<string, Project>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<string, Project>>().Invoke("prj6"));

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("Name", "ABBR", "Cust"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("prj6");
        await repo.Received(1).AddAsync(Arg.Any<Func<string, Project>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BlankName_ReturnsInvalid_AndDoesNotPersist()
    {
        var repo = Substitute.For<IProjectRepository>();

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("", "ABBR", "Cust"));

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().ContainKey(nameof(CreateProjectCommand.Name));
        await repo.DidNotReceive().AddAsync(Arg.Any<Func<string, Project>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateAbbreviation_ReturnsConflict()
    {
        var repo = Substitute.For<IProjectRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Project.Create("prj1", "X", "ABBR", "C") });

        var result = await Build(repo).HandleAsync(new CreateProjectCommand("Name", "ABBR", "Cust"));

        result.Status.Should().Be(ResultStatus.Conflict);
        await repo.DidNotReceive().AddAsync(Arg.Any<Func<string, Project>>(), Arg.Any<CancellationToken>());
    }
}
