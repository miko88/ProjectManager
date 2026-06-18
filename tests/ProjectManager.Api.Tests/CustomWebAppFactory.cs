using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProjectManager.Api.Tests;

public sealed class CustomWebAppFactory : WebApplicationFactory<Program>
{
    // Test-only fixture credential. Defined once; the hash is computed at runtime so
    // there is no pasted magic value and no plaintext-in-comment to drift out of sync.
    public const string TestUser = "admin";
    public const string TestPassword = "Admin123!";

    public string DataFile { get; } = Path.Combine(Path.GetTempPath(), $"pm-api-{Guid.NewGuid():N}.xml");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        File.WriteAllText(DataFile, """
            <?xml version="1.0" encoding="utf-8"?>
            <projects>
              <project id="prj1"><name>Seed</name><abbreviation>SEED</abbreviation><customer>Cust</customer></project>
            </projects>
            """);

        var passwordHash = new PasswordHasher<string>().HashPassword(TestUser, TestPassword);

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:ProjectsFilePath"] = DataFile,
                ["Auth:SigningKey"] = "integration-test-signing-key-min-32-characters-1234",
                ["Auth:Username"] = TestUser,
                ["Auth:PasswordHash"] = passwordHash
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (File.Exists(DataFile)) File.Delete(DataFile);
        base.Dispose(disposing);
    }
}
