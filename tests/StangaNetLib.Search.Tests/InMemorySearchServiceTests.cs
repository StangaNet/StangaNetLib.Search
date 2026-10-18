using StangaNetLib.Core.Common;
using StangaNetLib.Search.Abstractions;
using StangaNetLib.Search.Implementations;
using StangaNetLib.Search.Models;

namespace StangaNetLib.Search.Tests;

public sealed class InMemorySearchServiceTests
{
    private readonly InMemoryDocumentStore _store = new();
    private const string TestIndex = "test-index";

    private ISearchService<TestProduct> CreateService()
        => new InMemorySearchService<TestProduct>(_store, TestIndex);

    [Fact]
    public async Task IndexDocument_NewDocument_Succeeds()
    {
        var sut = CreateService();

        var result = await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop Pro", "Electronics", 999.99m));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetDocument_AfterIndex_ReturnsDocument()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop Pro", "Electronics", 999.99m));

        var result = await sut.GetDocumentAsync("1");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Laptop Pro");
        result.Value.Category.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetDocument_WhenNotFound_ReturnsNotFoundError()
    {
        var sut = CreateService();

        var result = await sut.GetDocumentAsync("nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task IndexDocument_ReplacesExistingDocument()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Old Name", "Electronics", 100m));

        await sut.IndexDocumentAsync("1", new TestProduct("1", "New Name", "Electronics", 200m));

        var result = await sut.GetDocumentAsync("1");
        result.Value!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task DeleteDocument_ExistingDocument_Succeeds()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));

        var result = await sut.DeleteDocumentAsync("1");

        result.IsSuccess.Should().BeTrue();
        var get = await sut.GetDocumentAsync("1");
        get.IsFailure.Should().BeTrue();
        get.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteDocument_WhenNotFound_ReturnsNotFoundError()
    {
        var sut = CreateService();

        var result = await sut.DeleteDocumentAsync("nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task IndexDocuments_BatchIndex_StoresAllDocuments()
    {
        var sut = CreateService();
        var docs = new (string, TestProduct)[]
        {
            ("1", new TestProduct("1", "Laptop", "Electronics", 999m)),
            ("2", new TestProduct("2", "Mouse", "Electronics", 29m)),
            ("3", new TestProduct("3", "Desk", "Furniture", 299m)),
        };

        var result = await sut.IndexDocumentsAsync(docs);

        result.IsSuccess.Should().BeTrue();
        var search = await sut.SearchAsync(new SearchQuery());
        search.Value!.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task SearchAsync_WithMatchingTerm_ReturnsMatchingDocuments()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop Pro", "Electronics", 999m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Gaming Mouse", "Electronics", 49m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Desk", "Furniture", 299m));

        var result = await sut.SearchAsync(new SearchQuery { Term = "Laptop" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Hits[0].Document.Name.Should().Be("Laptop Pro");
    }

    [Fact]
    public async Task SearchAsync_WithNoTerm_ReturnsAllDocuments()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Mouse", "Electronics", 49m));

        var result = await sut.SearchAsync(new SearchQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_EmptyIndex_ReturnsEmptyResult()
    {
        var sut = CreateService();

        var result = await sut.SearchAsync(new SearchQuery { Term = "anything" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithSingleFilterValue_FiltersDocuments()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Mouse", "Electronics", 49m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Desk", "Furniture", 299m));

        var result = await sut.SearchAsync(new SearchQuery
        {
            Filters = new Dictionary<string, string[]> { ["Category"] = ["Electronics"] }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Hits.All(h => h.Document.Category == "Electronics").Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WithMultipleFilterValues_AppliesOrWithinField()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Desk", "Furniture", 299m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Book", "Media", 19m));

        var result = await sut.SearchAsync(new SearchQuery
        {
            Filters = new Dictionary<string, string[]> { ["Category"] = ["Electronics", "Furniture"] }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchAsync_WithMultipleFilters_AppliesAndAcrossFields()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Apple Laptop", "Electronics", 999m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Apple Mouse", "Electronics", 49m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Gaming Laptop", "Electronics", 799m));

        var result = await sut.SearchAsync(new SearchQuery
        {
            Term = "Laptop",
            Filters = new Dictionary<string, string[]> { ["Category"] = ["Electronics"] }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Hits.All(h => h.Document.Name.Contains("Laptop")).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WithFacets_ReturnsFacetCounts()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Mouse", "Electronics", 49m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Desk", "Furniture", 299m));

        var result = await sut.SearchAsync(new SearchQuery { Facets = ["Category"] });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Facets.Should().ContainKey("Category");
        var facet = result.Value.Facets["Category"];
        facet.Buckets.Should().HaveCount(2);
        facet.Buckets.First(b => b.Value == "Electronics").Count.Should().Be(2);
        facet.Buckets.First(b => b.Value == "Furniture").Count.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_FacetsBuckets_OrderedByCountDescending()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "P1", "Electronics", 10m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "P2", "Electronics", 20m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "P3", "Electronics", 30m));
        await sut.IndexDocumentAsync("4", new TestProduct("4", "P4", "Furniture", 40m));

        var result = await sut.SearchAsync(new SearchQuery { Facets = ["Category"] });

        result.Value!.Facets["Category"].Buckets[0].Value.Should().Be("Electronics");
        result.Value.Facets["Category"].Buckets[0].Count.Should().Be(3);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ReturnsCorrectPage()
    {
        var sut = CreateService();
        for (var i = 1; i <= 7; i++)
            await sut.IndexDocumentAsync(i.ToString(), new TestProduct(i.ToString(), $"Product {i}", "Test", i * 10m));

        var result = await sut.SearchAsync(new SearchQuery { PageNumber = 2, PageSize = 3 });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(7);
        result.Value.Hits.Should().HaveCount(3);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(3);
    }

    [Fact]
    public async Task SearchAsync_LastPage_ReturnsRemainingDocuments()
    {
        var sut = CreateService();
        for (var i = 1; i <= 5; i++)
            await sut.IndexDocumentAsync(i.ToString(), new TestProduct(i.ToString(), $"Product {i}", "Test", i * 10m));

        var result = await sut.SearchAsync(new SearchQuery { PageNumber = 3, PageSize = 2 });

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(5);
        result.Value.Hits.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_SortByFieldAscending_ReturnsOrderedResults()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Charlie", "Electronics", 100m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Alice", "Electronics", 200m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Bob", "Electronics", 150m));

        var result = await sut.SearchAsync(new SearchQuery { SortBy = "Name" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits[0].Document.Name.Should().Be("Alice");
        result.Value.Hits[1].Document.Name.Should().Be("Bob");
        result.Value.Hits[2].Document.Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task SearchAsync_SortByFieldDescending_ReturnsReverseOrderedResults()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Charlie", "Electronics", 100m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Alice", "Electronics", 200m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Bob", "Electronics", 150m));

        var result = await sut.SearchAsync(new SearchQuery { SortBy = "Name", SortDescending = true });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits[0].Document.Name.Should().Be("Charlie");
        result.Value.Hits[1].Document.Name.Should().Be("Bob");
        result.Value.Hits[2].Document.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task SearchAsync_TermSearch_HigherFrequencyDocumentScoresHigher()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop Laptop Laptop Pro", "Electronics", 999m));
        await sut.IndexDocumentAsync("2", new TestProduct("2", "Gaming Laptop", "Electronics", 699m));
        await sut.IndexDocumentAsync("3", new TestProduct("3", "Mouse", "Electronics", 29m));

        var result = await sut.SearchAsync(new SearchQuery { Term = "laptop" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits.Should().HaveCount(2);
        result.Value.Hits[0].Document.Id.Should().Be("1");
        result.Value.Hits[0].Score.Should().BeGreaterThan(result.Value.Hits[1].Score);
    }

    [Fact]
    public async Task SearchAsync_TermSearch_ScoreIsPositive()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));

        var result = await sut.SearchAsync(new SearchQuery { Term = "laptop" });

        result.Value!.Hits[0].Score.Should().BeGreaterThan(0f);
    }

    [Fact]
    public async Task SearchAsync_NoTermSearch_ScoreIsZero()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));

        var result = await sut.SearchAsync(new SearchQuery());

        result.Value!.Hits[0].Score.Should().Be(0f);
    }

    [Fact]
    public async Task SearchAsync_PageNumberZero_ReturnsValidationError()
    {
        var sut = CreateService();

        var result = await sut.SearchAsync(new SearchQuery { PageNumber = 0 });

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task SearchAsync_PageSizeZero_ReturnsValidationError()
    {
        var sut = CreateService();

        var result = await sut.SearchAsync(new SearchQuery { PageSize = 0 });

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task SearchAsync_PageSizeOverLimit_ReturnsValidationError()
    {
        var sut = CreateService();

        var result = await sut.SearchAsync(new SearchQuery { PageSize = 1001 });

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task SearchAsync_ResultIncludesTookMilliseconds()
    {
        var sut = CreateService();
        await sut.IndexDocumentAsync("1", new TestProduct("1", "Laptop", "Electronics", 999m));

        var result = await sut.SearchAsync(new SearchQuery());

        result.Value!.TookMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }

    private sealed record TestProduct(string Id, string Name, string Category, decimal Price);
}
