using System.Diagnostics;
using System.Text.Json;
using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Search.Abstractions;
using StangaNetLib.Search.Models;

namespace StangaNetLib.Search.Implementations;

internal sealed class InMemorySearchService<TDocument> : ISearchService<TDocument>
    where TDocument : class
{
    private readonly InMemoryDocumentStore _store;
    private readonly string _indexName;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal InMemorySearchService(InMemoryDocumentStore store, string indexName)
    {
        _store = Guard.Against.Null(store, nameof(store));
        _indexName = Guard.Against.NullOrWhiteSpace(indexName, nameof(indexName));
    }

    public Task<Result> IndexDocumentAsync(
        string documentId, TDocument document, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(documentId, nameof(documentId));
        Guard.Against.Null(document, nameof(document));
        _store.Upsert(_indexName, documentId, JsonSerializer.Serialize(document));
        return Task.FromResult(Result.Success());
    }

    public Task<Result> IndexDocumentsAsync(
        IEnumerable<(string Id, TDocument Document)> documents, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(documents, nameof(documents));
        foreach (var (id, doc) in documents)
        {
            Guard.Against.NullOrWhiteSpace(id, nameof(id));
            Guard.Against.Null(doc, nameof(doc));
            _store.Upsert(_indexName, id, JsonSerializer.Serialize(doc));
        }
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(documentId, nameof(documentId));
        return Task.FromResult(
            _store.Delete(_indexName, documentId)
                ? Result.Success()
                : Result.Failure(Error.NotFound("Document", documentId)));
    }

    public Task<Result<TDocument>> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(documentId, nameof(documentId));
        var json = _store.Get(_indexName, documentId);
        if (json is null)
            return Task.FromResult(Result<TDocument>.Failure(Error.NotFound("Document", documentId)));

        var doc = JsonSerializer.Deserialize<TDocument>(json, JsonOptions);
        return Task.FromResult(doc is null
            ? Result<TDocument>.Failure(Error.Internal($"Failed to deserialize document '{documentId}'."))
            : Result<TDocument>.Success(doc));
    }

    public Task<Result<SearchResult<TDocument>>> SearchAsync(
        SearchQuery query, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(query, nameof(query));

        if (query.PageNumber < 1)
            return Task.FromResult(Result<SearchResult<TDocument>>.Failure(
                Error.Validation(nameof(query.PageNumber), "PageNumber must be at least 1.")));
        if (query.PageSize < 1 || query.PageSize > 1000)
            return Task.FromResult(Result<SearchResult<TDocument>>.Failure(
                Error.Validation(nameof(query.PageSize), "PageSize must be between 1 and 1000.")));

        var sw = Stopwatch.StartNew();
        var allDocs = _store.GetAll(_indexName);

        var scored = ScoreDocuments(allDocs, query.Term);
        var filtered = ApplyFilters(scored, query.Filters);
        var facets = ComputeFacets(filtered, query.Facets);
        var sorted = Sort(filtered, query);

        var totalCount = sorted.Count;
        var skip = (query.PageNumber - 1) * query.PageSize;
        var page = sorted.Skip(skip).Take(query.PageSize);

        var hits = page.Select(d =>
        {
            var doc = JsonSerializer.Deserialize<TDocument>(d.Json, JsonOptions)!;
            return new SearchHit<TDocument>(doc, d.Id, d.Score);
        }).ToList();

        sw.Stop();

        var result = new SearchResult<TDocument>
        {
            Hits = hits,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            Facets = facets,
            TookMilliseconds = sw.ElapsedMilliseconds,
        };

        return Task.FromResult(Result<SearchResult<TDocument>>.Success(result));
    }

    private static List<ScoredDocument> ScoreDocuments(IReadOnlyList<(string Id, string Json)> docs, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return docs.Select(d => new ScoredDocument(d.Id, d.Json, 0f)).ToList();

        var termLower = term.ToLowerInvariant();
        var matching = docs.Where(d => d.Json.ToLowerInvariant().Contains(termLower)).ToList();
        if (matching.Count == 0) return [];

        var totalDocs = docs.Count;
        var termDocCount = matching.Count;
        var avgLen = docs.Average(d => (double)d.Json.Length);

        return matching
            .Select(d => new ScoredDocument(
                d.Id, d.Json, ComputeBm25(d.Json, termLower, totalDocs, termDocCount, avgLen)))
            .Where(d => d.Score > 0f)
            .ToList();
    }

    private static float ComputeBm25(
        string docJson, string termLower, int totalDocs, int termDocCount, double avgDocLen)
    {
        const double k1 = 1.5;
        const double b = 0.75;

        var tf = CountOccurrences(docJson.ToLowerInvariant(), termLower);
        if (tf == 0) return 0f;

        // IDF with smoothing to avoid negative values when termDocCount >= totalDocs
        var idf = Math.Log((totalDocs - termDocCount + 0.5) / (termDocCount + 0.5) + 1.0);
        var tfNorm = tf * (k1 + 1.0) / (tf + k1 * (1.0 - b + b * docJson.Length / avgDocLen));
        return (float)(idf * tfNorm);
    }

    private static int CountOccurrences(string text, string term)
    {
        int count = 0, pos = 0;
        while ((pos = text.IndexOf(term, pos, StringComparison.Ordinal)) >= 0)
        {
            count++;
            pos += term.Length;
        }
        return count;
    }

    private static List<ScoredDocument> ApplyFilters(
        List<ScoredDocument> docs, IReadOnlyDictionary<string, string[]> filters)
    {
        if (filters.Count == 0) return docs;
        return docs.Where(d => MatchesFilters(d.Json, filters)).ToList();
    }

    private static bool MatchesFilters(string json, IReadOnlyDictionary<string, string[]> filters)
    {
        Dictionary<string, JsonElement>? dict;
        try { dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions); }
        catch { return false; }
        if (dict is null) return false;

        foreach (var (field, values) in filters)
        {
            if (!dict.TryGetValue(field, out var elem)) return false;
            var fieldVal = elem.ToString();
            if (!values.Any(v => string.Equals(v, fieldVal, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        return true;
    }

    private static List<ScoredDocument> Sort(List<ScoredDocument> docs, SearchQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            return query.SortDescending
                ? docs.OrderByDescending(d => GetFieldString(d.Json, query.SortBy)).ToList()
                : docs.OrderBy(d => GetFieldString(d.Json, query.SortBy)).ToList();
        }
        // Default: score descending (0 for no-term queries), then stable by ID
        return docs.OrderByDescending(d => d.Score).ThenBy(d => d.Id).ToList();
    }

    private static string GetFieldString(string json, string field)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions);
            return dict?.TryGetValue(field, out var v) == true ? v.ToString() : string.Empty;
        }
        catch { return string.Empty; }
    }

    private static IReadOnlyDictionary<string, FacetResult> ComputeFacets(
        List<ScoredDocument> docs, string[] facetFields)
    {
        if (facetFields.Length == 0) return new Dictionary<string, FacetResult>();

        var result = new Dictionary<string, FacetResult>(facetFields.Length);
        foreach (var field in facetFields)
        {
            var buckets = docs
                .Select(d => GetFieldString(d.Json, field))
                .Where(v => v.Length > 0)
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Select(g => new FacetBucket(g.Key, g.LongCount()))
                .OrderByDescending(b => b.Count)
                .ToList();

            result[field] = new FacetResult(field, buckets);
        }
        return result;
    }

    private sealed record ScoredDocument(string Id, string Json, float Score);
}
