using System.Net.Http.Json;
using ProjectManager.Contracts;

namespace ProjectManager.Client.Services;

public sealed class AuthApiClient(HttpClient http)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await http.PostAsJsonAsync("/auth/login", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<LoginResponse>()
            : null;
    }
}
