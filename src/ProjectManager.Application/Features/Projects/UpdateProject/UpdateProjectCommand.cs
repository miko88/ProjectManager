namespace ProjectManager.Application.Features.Projects.UpdateProject;

public sealed record UpdateProjectCommand(string Id, string Name, string Abbreviation, string Customer);
