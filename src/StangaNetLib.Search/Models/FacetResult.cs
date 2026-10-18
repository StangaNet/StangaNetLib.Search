namespace StangaNetLib.Search.Models;

/// <summary>Aggregated facet counts for a single field.</summary>
/// <param name="FieldName">The field that was faceted.</param>
/// <param name="Buckets">Distinct value buckets, ordered by count descending.</param>
public sealed record FacetResult(string FieldName, IReadOnlyList<FacetBucket> Buckets);
