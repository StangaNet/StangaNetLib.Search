using System.Collections.Concurrent;

namespace StangaNetLib.Search.Implementations;

/// <summary>
/// Thread-safe in-memory document store shared across all InMemory search service instances.
/// Documents are stored as serialized JSON strings keyed by index name and document ID.
/// </summary>
internal sealed class InMemoryDocumentStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _indexes = new();

    internal void Upsert(string indexName, string documentId, string json)
    {
        var index = _indexes.GetOrAdd(indexName, _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));
        index[documentId] = json;
    }

    internal bool Delete(string indexName, string documentId)
        => _indexes.TryGetValue(indexName, out var index) && index.TryRemove(documentId, out _);

    internal string? Get(string indexName, string documentId)
    {
        if (!_indexes.TryGetValue(indexName, out var index)) return null;
        index.TryGetValue(documentId, out var json);
        return json;
    }

    internal IReadOnlyList<(string Id, string Json)> GetAll(string indexName)
    {
        if (!_indexes.TryGetValue(indexName, out var index)) return [];
        return index.Select(kvp => (kvp.Key, kvp.Value)).ToList();
    }

    internal bool IndexExists(string indexName) => _indexes.ContainsKey(indexName);

    internal bool CreateIndex(string indexName)
        => _indexes.TryAdd(indexName, new ConcurrentDictionary<string, string>(StringComparer.Ordinal));

    internal bool DeleteIndex(string indexName) => _indexes.TryRemove(indexName, out _);

    internal bool Reindex(string sourceIndex, string targetIndex)
    {
        if (!_indexes.TryGetValue(sourceIndex, out var source)) return false;
        var target = _indexes.GetOrAdd(targetIndex, _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));
        foreach (var (id, json) in source)
            target[id] = json;
        return true;
    }
}
