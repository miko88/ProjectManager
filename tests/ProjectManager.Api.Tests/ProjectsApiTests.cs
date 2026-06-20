using FluentAssertions;
using ProjectManager.Contracts;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProjectManager.Api.Tests;

public class ProjectsApiTests(CustomWebAppFactory factory) : IClassFixture<CustomWebAppFactory>
{
    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(CustomWebAppFactory.TestUser, CustomWebAppFactory.TestPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task GetProjects_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/projects", TestContext.Current.CancellationToken);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithBadPassword_Returns401()
    {
        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/auth/login", new LoginRequest(CustomWebAppFactory.TestUser, "wrong"), cancellationToken: TestContext.Current.CancellationToken);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjects_WithToken_ReturnsSeed()
    {
        var client = await AuthedClientAsync();
        var projects = await client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", cancellationToken: TestContext.Current.CancellationToken);
        projects.Should().NotBeNull();
        projects!.Should().Contain(p => p.Id == "prj1");
    }

    [Fact]
    public async Task CreateProject_WithBlankName_Returns400ValidationProblem()
    {
        var client = await AuthedClientAsync();
        var res = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("", "X", "C"), cancellationToken: TestContext.Current.CancellationToken);
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateThenDeleteProject_Works()
    {
        var client = await AuthedClientAsync();

        var create = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("New", "NEWABBR", "Cust"), cancellationToken: TestContext.Current.CancellationToken);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ProjectDto>(cancellationToken: TestContext.Current.CancellationToken);

        var delete = await client.DeleteAsync($"/api/projects/{created!.Id}", TestContext.Current.CancellationToken);
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
