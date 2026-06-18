namespace ProjectManager.Contracts;

public sealed record CreateProjectRequest(string Name, string Abbreviation, string Customer);
public sealed record UpdateProjectRequest(string Name, string Abbreviation, string Customer);
