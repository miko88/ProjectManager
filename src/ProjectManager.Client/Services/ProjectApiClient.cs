using System.Net.Http.Json;
using ProjectManager.Contracts;

namespace ProjectManager.Client.Services;

public sealed class ProjectApiClient(HttpClient http)
{
    public async Task<List<ProjectDto>> GetAllAsync() =>
        await http.GetFromJsonAsync<List<ProjectDto>>("/api/projects") ?? new();

    public async Task<HttpResponseMessage> CreateAsync(CreateProjectRequest req) =>
        await http.PostAsJsonAsync("/api/projects", req);

    public async Task<HttpResponseMessage> UpdateAsync(string id, UpdateProjectRequest req) =>
        await http.PutAsJsonAsync($"/api/projects/{id}", req);

    public async Task<HttpResponseMessage> DeleteAsync(string id) =>
        await http.DeleteAsync($"/api/projects/{id}");
}
