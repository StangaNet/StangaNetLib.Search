using StangaNetLib.Core.Common;
using StangaNetLib.Core.Guards;
using StangaNetLib.Search.Abstractions;

namespace StangaNetLib.Search.Implementations;

internal sealed class InMemorySearchIndexManager : ISearchIndexManager
{
    private readonly InMemoryDocumentStore _store;

    internal InMemorySearchIndexManager(InMemoryDocumentStore store)
    {
        _store = Guard.Against.Null(store, nameof(store));
    }

    public Task<Result> CreateIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(indexName, nameof(indexName));
        if (_store.IndexExists(indexName))
            return Task.FromResult(Result.Failure(Error.Conflict(
                "Index.AlreadyExists", $"Index '{indexName}' already exists.")));
        _store.CreateIndex(indexName);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(indexName, nameof(indexName));
        return Task.FromResult(
            _store.DeleteIndex(indexName)
                ? Result.Success()
                : Result.Failure(Error.NotFound("Index", indexName)));
    }

    public Task<Result<bool>> IndexExistsAsync(string indexName, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(indexName, nameof(indexName));
        return Task.FromResult(Result<bool>.Success(_store.IndexExists(indexName)));
    }

    public Task<Result> ReindexAsync(string sourceIndex, string targetIndex, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(sourceIndex, nameof(sourceIndex));
        Guard.Against.NullOrWhiteSpace(targetIndex, nameof(targetIndex));
        return Task.FromResult(
            _store.Reindex(sourceIndex, targetIndex)
                ? Result.Success()
                : Result.Failure(Error.NotFound("Index", sourceIndex)));
    }
}
