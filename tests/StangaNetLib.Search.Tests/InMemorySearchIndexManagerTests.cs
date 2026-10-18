using StangaNetLib.Core.Common;
using StangaNetLib.Search.Abstractions;
using StangaNetLib.Search.Implementations;

namespace StangaNetLib.Search.Tests;

public sealed class InMemorySearchIndexManagerTests
{
    private readonly InMemoryDocumentStore _store = new();

    private ISearchIndexManager CreateManager() => new InMemorySearchIndexManager(_store);

    [Fact]
    public async Task CreateIndex_NewIndex_Succeeds()
    {
        var sut = CreateManager();

        var result = await sut.CreateIndexAsync("products");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateIndex_DuplicateIndex_ReturnsConflictError()
    {
        var sut = CreateManager();
        await sut.CreateIndexAsync("products");

        var result = await sut.CreateIndexAsync("products");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task IndexExists_AfterCreate_ReturnsTrue()
    {
        var sut = CreateManager();
        await sut.CreateIndexAsync("products");

        var result = await sut.IndexExistsAsync("products");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IndexExists_NonExistentIndex_ReturnsFalse()
    {
        var sut = CreateManager();

        var result = await sut.IndexExistsAsync("nonexistent");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteIndex_ExistingIndex_Succeeds()
    {
        var sut = CreateManager();
        await sut.CreateIndexAsync("products");

        var result = await sut.DeleteIndexAsync("products");

        result.IsSuccess.Should().BeTrue();
        var exists = await sut.IndexExistsAsync("products");
        exists.Value.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteIndex_NonExistentIndex_ReturnsNotFoundError()
    {
        var sut = CreateManager();

        var result = await sut.DeleteIndexAsync("nonexistent");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ReindexAsync_CopiesDocumentsFromSourceToTarget()
    {
        var sut = CreateManager();
        _store.Upsert("source", "doc1", "{\"Id\":\"1\",\"Name\":\"Alpha\"}");
        _store.Upsert("source", "doc2", "{\"Id\":\"2\",\"Name\":\"Beta\"}");

        var result = await sut.ReindexAsync("source", "target");

        result.IsSuccess.Should().BeTrue();
        _store.GetAll("target").Should().HaveCount(2);
    }

    [Fact]
    public async Task ReindexAsync_CreatesTargetIndexIfAbsent()
    {
        var sut = CreateManager();
        _store.Upsert("source", "doc1", "{\"Id\":\"1\"}");

        await sut.ReindexAsync("source", "new-target");

        var exists = await sut.IndexExistsAsync("new-target");
        exists.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ReindexAsync_NonExistentSource_ReturnsNotFoundError()
    {
        var sut = CreateManager();

        var result = await sut.ReindexAsync("nonexistent", "target");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DeleteIndex_RemovesDocumentsFromStore()
    {
        var sut = CreateManager();
        await sut.CreateIndexAsync("products");
        _store.Upsert("products", "doc1", "{}");

        await sut.DeleteIndexAsync("products");

        _store.IndexExists("products").Should().BeFalse();
    }
}
