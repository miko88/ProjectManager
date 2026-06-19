using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProjectManager.Domain;
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
    public async Task Add_AssignsId_ThenGetById_RoundTrips()
    {
        var repo = Build();
        var created = await repo.AddAsync(id => Project.Create(id, "New", "NEW", "Cust"));

        created.Id.Should().Be("prj1");
        var loaded = await repo.GetByIdAsync(created.Id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("New");
    }

    [Fact]
    public async Task Add_AssignsNextIdAsMaxPlusOne()
    {
        await SeedAsync("""
            <projects>
              <project id="prj3"><name>A</name><abbreviation>A</abbreviation><customer>C</customer></project>
              <project id="prj7"><name>B</name><abbreviation>B</abbreviation><customer>C</customer></project>
            </projects>
            """);

        var created = await Build().AddAsync(id => Project.Create(id, "C", "CC", "Cust"));
        created.Id.Should().Be("prj8");
    }

    [Fact]
    public async Task ConcurrentAdds_AssignUniqueIds_AndDoNotCorruptFile()
    {
        var repo = Build();

        var tasks = Enumerable.Range(0, 20).Select(i =>
            repo.AddAsync(id => Project.Create(id, $"N{i}", $"AB{i}", "C")));
        var created = await Task.WhenAll(tasks);

        var all = await repo.GetAllAsync();
        all.Should().HaveCount(20);
        // Atomic id generation inside the write lock => no two concurrent creates collide.
        created.Select(p => p.Id).Distinct().Should().HaveCount(20);
    }

    public void Dispose()
    {
        if (File.Exists(_file)) File.Delete(_file);
    }
}
