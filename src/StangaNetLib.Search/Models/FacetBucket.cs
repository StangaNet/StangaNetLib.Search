namespace StangaNetLib.Search.Models;

/// <summary>A single value bucket within a <see cref="FacetResult"/>.</summary>
/// <param name="Value">The field value for this bucket.</param>
/// <param name="Count">Number of documents that have this value.</param>
public sealed record FacetBucket(string Value, long Count);
