namespace StangaNetLib.Search.Models;

/// <summary>
/// The result of a <see cref="StangaNetLib.Search.Abstractions.ISearchService{TDocument}.SearchAsync"/> call.
/// </summary>
/// <typeparam name="TDocument">The document type.</typeparam>
public sealed class SearchResult<TDocument>
{
    /// <summary>The matched document hits for the current page.</summary>
    public IReadOnlyList<SearchHit<TDocument>> Hits { get; init; } = [];

    /// <summary>Total number of matching documents across all pages.</summary>
    public long TotalCount { get; init; }

    /// <summary>The 1-based page number returned.</summary>
    public int PageNumber { get; init; }

    /// <summary>The page size used for this result.</summary>
    public int PageSize { get; init; }

    /// <summary>Facet value counts per field. Empty when no facets were requested.</summary>
    public IReadOnlyDictionary<string, FacetResult> Facets { get; init; } = new Dictionary<string, FacetResult>();

    /// <summary>Time in milliseconds the search backend took to execute the query.</summary>
    public long TookMilliseconds { get; init; }
}
