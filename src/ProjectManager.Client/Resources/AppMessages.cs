using System.Globalization;
using System.Resources;

namespace ProjectManager.Client.Resources;

/// <summary>
/// Strongly-typed access to the UI strings in <c>AppMessages.resx</c>. Backed by a
/// <see cref="ResourceManager"/> so the values live in a localizable resource rather than as
/// scattered string literals. The static properties also serve as DataAnnotations
/// <c>ErrorMessageResourceName</c> targets (via <c>ErrorMessageResourceType = typeof(AppMessages)</c>).
/// </summary>
public static class AppMessages
{
    private static readonly ResourceManager ResourceManager =
        new("ProjectManager.Client.Resources.AppMessages", typeof(AppMessages).Assembly);

    private static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string ServerUnavailable => Get(nameof(ServerUnavailable));
    public static string InvalidCredentials => Get(nameof(InvalidCredentials));
    public static string SaveFailed => Get(nameof(SaveFailed));
    public static string DuplicateAbbreviation => Get(nameof(DuplicateAbbreviation));
    public static string ProjectsLoadFailed => Get(nameof(ProjectsLoadFailed));
    public static string DeleteFailed => Get(nameof(DeleteFailed));

    /// <summary>Composite format string; argument {0} is the project name.</summary>
    public static string DeleteConfirm => Get(nameof(DeleteConfirm));

    public static string NameRequired => Get(nameof(NameRequired));
    public static string NameTooLong => Get(nameof(NameTooLong));
    public static string AbbreviationRequired => Get(nameof(AbbreviationRequired));
    public static string AbbreviationTooLong => Get(nameof(AbbreviationTooLong));
    public static string CustomerRequired => Get(nameof(CustomerRequired));
    public static string CustomerTooLong => Get(nameof(CustomerTooLong));
    public static string UsernameRequired => Get(nameof(UsernameRequired));
    public static string PasswordRequired => Get(nameof(PasswordRequired));
}
