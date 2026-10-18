using StangaNetLib.Core.Common;

namespace StangaNetLib.Search.Abstractions;

/// <summary>
/// Manages search index lifecycle: creation, deletion, existence checks, and reindexing.
/// </summary>
public interface ISearchIndexManager
{
    /// <summary>
    /// Creates a new index. Returns a conflict error when the index already exists.
    /// </summary>
    /// <param name="indexName">Name of the index to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> CreateIndexAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an index and all its documents.
    /// Returns a not-found error when the index does not exist.
    /// </summary>
    /// <param name="indexName">Name of the index to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> DeleteIndexAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether an index exists.
    /// </summary>
    /// <param name="indexName">Name of the index to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<bool>> IndexExistsAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies all documents from <paramref name="sourceIndex"/> into <paramref name="targetIndex"/>.
    /// Creates the target index if it does not already exist.
    /// Returns a not-found error when the source index does not exist.
    /// </summary>
    /// <param name="sourceIndex">Name of the source index.</param>
    /// <param name="targetIndex">Name of the target index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> ReindexAsync(string sourceIndex, string targetIndex, CancellationToken cancellationToken = default);
}
