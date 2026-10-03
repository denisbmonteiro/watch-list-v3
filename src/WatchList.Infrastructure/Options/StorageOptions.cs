namespace WatchList.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Folder holding the <c>.txt</c> lists; a relative path starts at the content root.</summary>
    public string DataPath { get; set; } = string.Empty;
}