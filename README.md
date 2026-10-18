# StangaNetLib.Search

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Search/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Search/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)

Unified search abstraction for the **StangaNetLib** ecosystem: `ISearchService<TDocument>` and `ISearchIndexManager`, plus an in-memory BM25 backend (filters, pagination, sorting, facets) for development and tests. Failures surface as `Result` / `Result<T>` from [`StangaNetLib.Core`](https://github.com/StangaNet/StangaNetLib.Core).

## Installation

The package is hosted on **GitHub Packages** (pulls Core from the same feed).

```xml
<!-- NuGet.config — source key must match your packageSourceCredentials -->
<configuration>
  <packageSources>
    <add key="StangaNet" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Search" Version="1.0.0" />
```

See [CHANGELOG.md](./CHANGELOG.md) for release history.

**Dependencies**: `StangaNetLib.Core` 1.0.0, `Microsoft.Extensions.*` 8.x.

## Contents

| Namespace | Types | Purpose |
|---|---|---|
| `StangaNetLib.Search.Abstractions` | `ISearchService<TDocument>`, `ISearchIndexManager` | Core search interfaces |
| `StangaNetLib.Search.Models` | `SearchQuery`, `SearchResult<T>`, `SearchHit<T>`, `FacetResult`, `FacetBucket` | Request and response models |
| `StangaNetLib.Search.Settings` | `SearchSettings` | Configuration for the in-memory backend |
| `StangaNetLib.Search.Implementations` | `InMemorySearchService<T>`, `InMemorySearchIndexManager`, `InMemoryDocumentStore` | In-memory backend |
| `StangaNetLib.Search.Extensions` | `ServiceCollectionExtensions` | DI registration |

## Quick Start

### 1. Configure appsettings.json

```json
{
  "Search": {
    "IndexName": "products"
  }
}
```

### 2. Register in Program.cs

```csharp
// From configuration
builder.Services.AddStangaNetLibSearch<ProductDocument>(builder.Configuration);

// Or with an explicit index name
builder.Services.AddStangaNetLibSearch<ProductDocument>("products");

// Multiple document types
builder.Services.AddStangaNetLibSearch<ProductDocument>("products");
builder.Services.AddStangaNetLibSearch<OrderDocument>("orders");
```

### 3. Inject and use

```csharp
public class ProductService(ISearchService<ProductDocument> search)
{
    public async Task<IReadOnlyList<ProductDocument>> FindAsync(string term, CancellationToken ct)
    {
        var result = await search.SearchAsync(new SearchQuery { Term = term }, ct);
        return result.Value!.Hits.Select(h => h.Document).ToList();
    }
}
```

## ISearchService\<TDocument\>

### SearchAsync

```csharp
var result = await search.SearchAsync(new SearchQuery
{
    Term = "laptop",
    Filters = new Dictionary<string, string[]>
    {
        ["Category"] = ["Electronics", "Computers"],
        ["Brand"]    = ["Apple"],
    },
    SortBy = "price",
    SortDescending = true,
    PageNumber = 1,
    PageSize = 20,
    Facets = ["Category", "Brand"],
}, cancellationToken);

if (result.IsSuccess)
{
    var sr = result.Value!;
    Console.WriteLine($"Total: {sr.TotalCount}, Took: {sr.TookMilliseconds}ms");
    foreach (var hit in sr.Hits)
        Console.WriteLine($"[{hit.Score:F3}] {hit.Id} — {hit.Document}");

    foreach (var (field, facet) in sr.Facets)
        foreach (var bucket in facet.Buckets)
            Console.WriteLine($"{field}/{bucket.Value}: {bucket.Count}");
}
```

### IndexDocumentAsync / IndexDocumentsAsync

```csharp
await search.IndexDocumentAsync("prod-1", new ProductDocument { Name = "MacBook Pro" }, ct);

await search.IndexDocumentsAsync(
[
    ("prod-1", new ProductDocument { Name = "MacBook Pro" }),
    ("prod-2", new ProductDocument { Name = "MacBook Air" }),
], ct);
```

### DeleteDocumentAsync / GetDocumentAsync

```csharp
var deleted = await search.DeleteDocumentAsync("prod-1", ct);
// deleted.Error.Type == ErrorType.NotFound when the document does not exist

var doc = await search.GetDocumentAsync("prod-1", ct);
if (doc.IsSuccess)
    Console.WriteLine(doc.Value!.Name);
```

## ISearchIndexManager

```csharp
public class IndexSetup(ISearchIndexManager manager)
{
    public async Task InitializeAsync(CancellationToken ct)
    {
        var exists = await manager.IndexExistsAsync("products", ct);
        if (!exists.Value)
            await manager.CreateIndexAsync("products", ct);
    }
}
```

## SearchQuery

| Property | Type | Default | Description |
|---|---|---|---|
| `Term` | `string?` | `null` | Full-text term; `null` matches all documents |
| `Filters` | `IReadOnlyDictionary<string, string[]>` | `{}` | Field filters (OR within field, AND across fields) |
| `SortBy` | `string?` | `null` | Field to sort by; `null` sorts by relevance |
| `SortDescending` | `bool` | `false` | Descending sort direction when `SortBy` is set |
| `PageNumber` | `int` | `1` | 1-based page number |
| `PageSize` | `int` | `20` | Results per page (1–1000) |
| `Facets` | `string[]` | `[]` | Fields for which to compute value counts |

## DI Registration

| Method | Description |
|---|---|
| `AddStangaNetLibSearch<TDocument>(string indexName)` | In-memory backend, explicit index name |
| `AddStangaNetLibSearch<TDocument>(IConfiguration)` | In-memory backend, index name from `Search:IndexName` config |

Registered services: `ISearchService<TDocument>` (singleton per document type), `ISearchIndexManager` (singleton, shared).

Provider packages (`StangaNetLib.Search.Elasticsearch` / `.Meilisearch`) are out of this repository; this package ships the contracts and in-memory implementation.

## License

Apache-2.0 — see [LICENSE](./LICENSE) and [NOTICE](./NOTICE).
