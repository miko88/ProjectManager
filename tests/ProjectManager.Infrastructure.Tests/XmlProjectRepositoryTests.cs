using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProjectManager.Application.Abstractions;
using ProjectManager.Application.Common;
using ProjectManager.Infrastructure.Storage;
using Xunit;

namespace ProjectManager.Infrastructure.Tests;

public class XmlProjectRepositoryTests : IDisposable
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"pm-{Guid.NewGuid():N}.xml");

    private XmlProjectRepository Build()
    {
        var options = Options.Create(new StorageOptions { ProjectsFilePath = _file });
        return new XmlProjectRepository(options, NullLogger<XmlProjectRepository>.Instance);
    }

    private async Task SeedAsync(string xml) => await File.WriteAllTextAsync(_file, xml);

    [Fact]
    public async Task GetAll_ReadsSeededProjects()
    {
        await SeedAsync("""
            <?xml version="1.0" encoding="utf-8"?>
            <projects>
              <project id="prj1"><name>A</name><abbreviation>A1</abbreviation><customer>C</customer></project>
            </projects>
            """);

        var all = await Build().GetAllAsync();

        all.Should().HaveCount(1);
        all[0].Id.Should().Be("prj1");
        all[0].Abbreviation.Should().Be("A1");
    }

    [Fact]
    public async Task MissingFile_ReturnsEmpty()
    {
        var all = await Build().GetAllAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task CorruptXml_ThrowsInvalidDataException()
    {
        await SeedAsync("<projects><project></broken>");
        var act = async () => await Build().GetAllAsync();
        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Create_AssignsId_ThenGetById_RoundTrips()
    {
        var repo = Build();
        var created = await repo.CreateAsync(new ProjectDraft("New", "NEW", "Cust"));

        created.IsSuccess.Should().BeTrue();
        created.Value!.Id.Should().Be("prj1");

        var loaded = await repo.GetByIdAsync("prj1");
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("New");
    }

    [Fact]
    public async Task Create_AssignsNextIdAsMaxPlusOne()
    {
        await SeedAsync("""
            <projects>
              <project id="prj3"><name>A</name><abbreviation>A</abbreviation><customer>C</customer></project>
              <project id="prj7"><name>B</name><abbreviation>B</abbreviation><customer>C</customer></project>
            </projects>
            """);

        var created = await Build().CreateAsync(new ProjectDraft("C", "CC", "Cust"));
        created.Value!.Id.Should().Be("prj8");
    }

    [Fact]
    public async Task Create_DuplicateAbbreviation_ReturnsConflict_AndDoesNotPersist()
    {
        var repo = Build();
        await repo.CreateAsync(new ProjectDraft("A", "DUP", "C"));

        var second = await repo.CreateAsync(new ProjectDraft("B", "dup", "C")); // case-insensitive

        second.Status.Should().Be(ResultStatus.Conflict);
        (await repo.GetAllAsync()).Should().HaveCount(1);
    }

    [Fact]
    public async Task ConcurrentCreates_DifferentAbbreviations_AllSucceedWithUniqueIds()
    {
        var repo = Build();

        var tasks = Enumerable.Range(0, 20).Select(i => repo.CreateAsync(new ProjectDraft($"N{i}", $"AB{i}", "C")));
        var results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.IsSuccess);
        results.Select(r => r.Value!.Id).Distinct().Should().HaveCount(20);
        (await repo.GetAllAsync()).Should().HaveCount(20);
    }

    [Fact]
    public async Task ConcurrentCreates_SameAbbreviation_OnlyOneSucceeds()
    {
        var repo = Build();

        var tasks = Enumerable.Range(0, 20).Select(_ => repo.CreateAsync(new ProjectDraft("N", "DUP", "C")));
        var results = await Task.WhenAll(tasks);

        // Atomic uniqueness under the write lock: exactly one create wins, the rest see the conflict.
        results.Count(r => r.IsSuccess).Should().Be(1);
        results.Count(r => r.Status == ResultStatus.Conflict).Should().Be(19);
        (await repo.GetAllAsync()).Should().HaveCount(1);
    }

    [Fact]
    public async Task Update_AppliesChange_WhenFound()
    {
        var repo = Build();
        var created = await repo.CreateAsync(new ProjectDraft("Old", "OLD", "C"));
        var id = created.Value!.Id;

        var result = await repo.UpdateAsync(id, new ProjectDraft("New", "NEW", "C"));

        result.IsSuccess.Should().BeTrue();
        (await repo.GetByIdAsync(id))!.Name.Should().Be("New");
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenMissing()
    {
        var result = await Build().UpdateAsync("nope", new ProjectDraft("N", "N", "C"));
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Update_DuplicateAbbreviation_ReturnsConflict_AndDoesNotPersist()
    {
        var repo = Build();
        var a = await repo.CreateAsync(new ProjectDraft("A", "AAA", "C"));
        await repo.CreateAsync(new ProjectDraft("B", "BBB", "C"));

        var result = await repo.UpdateAsync(a.Value!.Id, new ProjectDraft("A", "BBB", "C")); // collides with B

        result.Status.Should().Be(ResultStatus.Conflict);
        (await repo.GetByIdAsync(a.Value.Id))!.Abbreviation.Should().Be("AAA"); // unchanged
    }

    public void Dispose()
    {
        if (File.Exists(_file)) File.Delete(_file);
    }
}
