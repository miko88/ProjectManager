using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Abstractions;
using ProjectManager.Domain;

namespace ProjectManager.Infrastructure.Storage;

/// <summary>
/// XML-file implementation of <see cref="IProjectRepository"/>.
/// Reads through to disk on every call (small file, no stale-cache class of bugs).
/// Writes are serialized via a semaphore and committed atomically (temp file + replace)
/// so the store is never left half-written, even under concurrency.
/// </summary>
public sealed class XmlProjectRepository : IProjectRepository
{
    private readonly string _path;
    private readonly ILogger<XmlProjectRepository> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public XmlProjectRepository(IOptions<StorageOptions> options, ILogger<XmlProjectRepository> logger)
    {
        _path = options.Value.ProjectsFilePath;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
        {
            _logger.LogWarning("Projects file {Path} does not exist; returning empty list.", _path);
            return Array.Empty<Project>();
        }

        XDocument doc;
        try
        {
            await using var stream = File.OpenRead(_path);
            doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);
        }
        catch (XmlException ex)
        {
            _logger.LogError(ex, "Projects file {Path} is not valid XML.", _path);
            throw new InvalidDataException($"Projects file '{_path}' is corrupt.", ex);
        }

        return doc.Root?.Elements("project").Select(ToProject).ToList() ?? new List<Project>();
    }

    public async Task<Project?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Project> AddAsync(Func<string, Project> create, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var list = (await GetAllAsync(ct)).ToList();
            var project = create(NextId(list));   // id reserved + entity built under the lock
            list.Add(project);
            await SaveAtomicAsync(list, ct);
            return project;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private static string NextId(IReadOnlyList<Project> projects)
    {
        var max = projects
            .Select(p => p.Id.StartsWith("prj", StringComparison.Ordinal) &&
                         int.TryParse(p.Id.AsSpan(3), out var n)
                ? n
                : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"prj{max + 1}";
    }

    public Task UpdateAsync(Project project, CancellationToken ct = default) =>
        MutateAsync(list =>
        {
            var idx = list.FindIndex(p => p.Id == project.Id);
            if (idx < 0) throw new InvalidOperationException($"Project '{project.Id}' not found.");
            list[idx] = project;
        }, ct);

    public Task DeleteAsync(string id, CancellationToken ct = default) =>
        MutateAsync(list => list.RemoveAll(p => p.Id == id), ct);

    private async Task MutateAsync(Action<List<Project>> mutate, CancellationToken ct)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var list = (await GetAllAsync(ct)).ToList();
            mutate(list);
            await SaveAtomicAsync(list, ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task SaveAtomicAsync(IReadOnlyList<Project> projects, CancellationToken ct)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("projects",
                projects.Select(p => new XElement("project",
                    new XAttribute("id", p.Id),
                    new XElement("name", p.Name),
                    new XElement("abbreviation", p.Abbreviation),
                    new XElement("customer", p.Customer)))));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);

        var tmp = _path + ".tmp";
        try
        {
            await using (var stream = File.Create(tmp))
            {
                await doc.SaveAsync(stream, SaveOptions.None, ct);
            }

            // Atomic replace: the store is never observed half-written.
            if (File.Exists(_path))
                File.Replace(tmp, _path, null);
            else
                File.Move(tmp, _path);
        }
        catch
        {
            try
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);
            }
            catch
            {
                // best-effort cleanup; the original exception is what matters.
            }

            throw;
        }
    }

    private static Project ToProject(XElement e) => Project.Create(
        (string?)e.Attribute("id") ?? throw new InvalidDataException("project@id is missing."),
        (string?)e.Element("name") ?? "",
        (string?)e.Element("abbreviation") ?? "",
        (string?)e.Element("customer") ?? "");
}
