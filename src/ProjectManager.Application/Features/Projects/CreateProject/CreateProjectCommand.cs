namespace ProjectManager.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(string Name, string Abbreviation, string Customer);
