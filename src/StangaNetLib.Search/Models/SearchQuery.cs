namespace StangaNetLib.Search.Models;

/// <summary>
/// Represents a search request: full-text term, field filters, pagination, sorting, and facets.
/// </summary>
public sealed class SearchQuery
{
    /// <summary>
    /// Full-text search term. When <see langword="null"/> or empty, all documents match
    /// and results are ordered by <see cref="SortBy"/> or by document ID.
    /// </summary>
    public string? Term { get; init; }

    /// <summary>
    /// Field-level filters. Key is the field name; value is the list of accepted values
    /// (OR logic within a field, AND logic across fields).
    /// Example: <c>{ "Category": ["Electronics", "Computers"], "Brand": ["Apple"] }</c>
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Filters { get; init; } = new Dictionary<string, string[]>();

    /// <summary>
    /// Field name to sort by. When <see langword="null"/>, results are ordered by relevance
    /// score descending (when a <see cref="Term"/> is provided) or by document ID.
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// When <see langword="true"/> and <see cref="SortBy"/> is set, sorts in descending order.
    /// </summary>
    public bool SortDescending { get; init; }

    /// <summary>1-based page number. Defaults to <c>1</c>.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Number of results per page. Defaults to <c>20</c>. Must be between 1 and 1000.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Field names for which to compute facet value counts.
    /// An empty array disables facet computation.
    /// </summary>
    public string[] Facets { get; init; } = [];
}
