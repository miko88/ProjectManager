using ProjectManager.Domain;

namespace ProjectManager.Application.Abstractions;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default);
    Task<Project?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Adds a project, generating its id atomically. The <paramref name="create"/> factory is
    /// invoked with a freshly reserved id <i>inside</i> the write lock, so id generation and the
    /// write are a single atomic operation (no two concurrent creates can receive the same id).
    /// Returns the created project.
    /// </summary>
    Task<Project> AddAsync(Func<string, Project> create, CancellationToken ct = default);

    Task UpdateAsync(Project project, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
