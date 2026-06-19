namespace ProjectManager.Application.Common;

/// <summary>
/// Centralized application-layer result/error message text, so the same wording is not
/// duplicated across use-cases (e.g. not-found and duplicate-abbreviation appear in several handlers).
/// </summary>
public static class ResultMessages
{
    public const string InvalidCredentials = "Invalid username or password.";

    public static string ProjectNotFound(string id) => $"Project '{id}' was not found.";

    public static string DuplicateAbbreviation(string abbreviation) =>
        $"A project with abbreviation '{abbreviation}' already exists.";
}
