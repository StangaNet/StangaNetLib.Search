namespace StangaNetLib.Search.Models;

/// <summary>
/// A single document hit returned within a <see cref="SearchResult{TDocument}"/>.
/// </summary>
/// <typeparam name="TDocument">The document type.</typeparam>
/// <param name="Document">The matched document.</param>
/// <param name="DocumentId">The document's unique identifier within the index.</param>
/// <param name="Score">
/// Relevance score computed by the search backend. Higher values indicate greater relevance.
/// Zero when no <c>Term</c> was specified in the query.
/// </param>
public sealed record SearchHit<TDocument>(TDocument Document, string DocumentId, float Score);
