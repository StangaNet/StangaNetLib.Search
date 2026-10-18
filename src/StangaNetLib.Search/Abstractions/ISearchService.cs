using StangaNetLib.Core.Common;
using StangaNetLib.Search.Models;

namespace StangaNetLib.Search.Abstractions;

/// <summary>
/// Provides document indexing and search operations for a specific document type.
/// Each registration targets a single named index.
/// </summary>
/// <typeparam name="TDocument">The document type to index and search.</typeparam>
public interface ISearchService<TDocument> where TDocument : class
{
    /// <summary>
    /// Executes a search query and returns matching documents.
    /// Returns a validation error when <see cref="SearchQuery.PageNumber"/> is less than 1
    /// or <see cref="SearchQuery.PageSize"/> is outside the range [1, 1000].
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<SearchResult<TDocument>>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a single document, replacing any existing document with the same ID.
    /// </summary>
    /// <param name="documentId">Unique identifier for the document within the index.</param>
    /// <param name="document">The document to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> IndexDocumentAsync(
        string documentId,
        TDocument document,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes multiple documents in a single batch operation.
    /// Each document replaces any existing document with the same ID.
    /// </summary>
    /// <param name="documents">Sequence of (id, document) pairs to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> IndexDocumentsAsync(
        IEnumerable<(string Id, TDocument Document)> documents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document by its ID.
    /// Returns a not-found error when the document does not exist.
    /// </summary>
    /// <param name="documentId">The document ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> DeleteDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single document by its ID.
    /// Returns a not-found error when the document does not exist.
    /// </summary>
    /// <param name="documentId">The document ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<TDocument>> GetDocumentAsync(
        string documentId,
        CancellationToken cancellationToken = default);
}
