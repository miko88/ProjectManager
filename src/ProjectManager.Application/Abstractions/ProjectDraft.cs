namespace ProjectManager.Application.Abstractions;

/// <summary>
/// The mutable fields of a project, used as input to <see cref="IProjectRepository.CreateAsync"/>
/// and <see cref="IProjectRepository.UpdateAsync"/>. The repository assigns the id on create.
/// </summary>
public sealed record ProjectDraft(string Name, string Abbreviation, string Customer);
