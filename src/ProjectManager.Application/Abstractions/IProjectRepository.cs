using ProjectManager.Application.Common;
using ProjectManager.Domain;

namespace ProjectManager.Application.Abstractions;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default);
    Task<Project?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Creates a project: assigns the id and enforces abbreviation uniqueness atomically.
    /// Returns <c>Conflict</c> if the abbreviation is already used, otherwise <c>Success</c> with the
    /// created project. Each adapter guarantees atomicity its own way (XML: under a write lock;
    /// a SQL adapter: a unique constraint; a REST adapter: mapping a 409) — the contract is the
    /// intent and outcome, not how it is achieved.
    /// </summary>
    Task<Result<Project>> CreateAsync(ProjectDraft draft, CancellationToken ct = default);

    /// <summary>
    /// Updates the project with <paramref name="id"/>: returns <c>NotFound</c> if it does not exist,
    /// <c>Conflict</c> if the new abbreviation collides with another project, otherwise <c>Success</c>.
    /// Existence and uniqueness are enforced atomically by the adapter.
    /// </summary>
    Task<Result> UpdateAsync(string id, ProjectDraft draft, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}
