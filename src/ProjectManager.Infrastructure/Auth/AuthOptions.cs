namespace ProjectManager.Infrastructure.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Issuer { get; set; } = "ProjectManager";
    public string Audience { get; set; } = "ProjectManagerClient";
    public int TokenExpiryMinutes { get; set; } = 60;
    public string SigningKey { get; set; } = "";
}
