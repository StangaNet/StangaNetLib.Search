using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StangaNetLib.Search.Abstractions;
using StangaNetLib.Search.Extensions;

namespace StangaNetLib.Search.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStangaNetLibSearch_WithIndexName_RegistersSearchService()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>("test-index");

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<ISearchService<TestDocument>>();

        service.Should().NotBeNull();
    }

    [Fact]
    public void AddStangaNetLibSearch_WithIndexName_RegistersIndexManager()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>("test-index");

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<ISearchIndexManager>();

        manager.Should().NotBeNull();
    }

    [Fact]
    public void AddStangaNetLibSearch_MultipleDocumentTypes_RegistersBothServices()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>("documents");
        services.AddStangaNetLibSearch<OtherDocument>("others");

        var provider = services.BuildServiceProvider();

        provider.GetService<ISearchService<TestDocument>>().Should().NotBeNull();
        provider.GetService<ISearchService<OtherDocument>>().Should().NotBeNull();
    }

    [Fact]
    public void AddStangaNetLibSearch_MultipleDocumentTypes_ShareSingleIndexManager()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>("documents");
        services.AddStangaNetLibSearch<OtherDocument>("others");

        var provider = services.BuildServiceProvider();

        var m1 = provider.GetRequiredService<ISearchIndexManager>();
        var m2 = provider.GetRequiredService<ISearchIndexManager>();
        m1.Should().BeSameAs(m2);
    }

    [Fact]
    public void AddStangaNetLibSearch_WithConfiguration_RegistersSearchService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Search:IndexName"] = "products" })
            .Build();

        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>(config);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISearchService<TestDocument>>().Should().NotBeNull();
    }

    [Fact]
    public void AddStangaNetLibSearch_WithEmptyConfiguration_FallsBackToTypeName()
    {
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>(config);

        var provider = services.BuildServiceProvider();
        provider.GetService<ISearchService<TestDocument>>().Should().NotBeNull();
    }

    [Fact]
    public async Task AddStangaNetLibSearch_ServicesAreSingleton_SameInstanceReturned()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>("test-index");

        var provider = services.BuildServiceProvider();
        var s1 = provider.GetRequiredService<ISearchService<TestDocument>>();
        var s2 = provider.GetRequiredService<ISearchService<TestDocument>>();

        s1.Should().BeSameAs(s2);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddStangaNetLibSearch_DocumentsPersistedAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibSearch<TestDocument>("scoped-test");
        var provider = services.BuildServiceProvider();

        var s1 = provider.GetRequiredService<ISearchService<TestDocument>>();
        await s1.IndexDocumentAsync("1", new TestDocument("1", "Hello"));

        var s2 = provider.GetRequiredService<ISearchService<TestDocument>>();
        var result = await s2.GetDocumentAsync("1");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Hello");
    }

    private sealed record TestDocument(string Id, string Name);
    private sealed record OtherDocument(string Id, string Title);
}
