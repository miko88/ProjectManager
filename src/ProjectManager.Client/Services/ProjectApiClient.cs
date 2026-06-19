using System.Net.Http.Json;
using ProjectManager.Contracts;

namespace ProjectManager.Client.Services;

public sealed class ProjectApiClient(HttpClient http)
{
    /// <summary>Returns the projects, or <c>null</c> if the request was not successful
    /// (e.g. 401) so the caller can show a friendly message instead of throwing.</summary>
    public async Task<List<ProjectDto>?> GetAllAsync()
    {
        using var response = await http.GetAsync("/api/projects");
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<List<ProjectDto>>() ?? new();
    }

    public async Task<HttpResponseMessage> CreateAsync(CreateProjectRequest req) =>
        await http.PostAsJsonAsync("/api/projects", req);

    public async Task<HttpResponseMessage> UpdateAsync(string id, UpdateProjectRequest req) =>
        await http.PutAsJsonAsync($"/api/projects/{id}", req);

    public async Task<HttpResponseMessage> DeleteAsync(string id) =>
        await http.DeleteAsync($"/api/projects/{id}");
}
