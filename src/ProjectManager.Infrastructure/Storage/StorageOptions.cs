namespace ProjectManager.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string ProjectsFilePath { get; set; } = "data/projects.xml";
}
