namespace StangaNetLib.Search.Settings;

/// <summary>
/// Configuration for a search index registration.
/// </summary>
public sealed class SearchSettings
{
    /// <summary>Default configuration section key.</summary>
    public const string SectionName = "Search";

    /// <summary>
    /// Name of the search index. When empty, the document type's simple name
    /// (lowercased) is used as the default index name.
    /// </summary>
    public string IndexName { get; set; } = string.Empty;
}
