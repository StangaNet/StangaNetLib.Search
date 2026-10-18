using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StangaNetLib.Core.Guards;
using StangaNetLib.Search.Abstractions;
using StangaNetLib.Search.Implementations;
using StangaNetLib.Search.Settings;

namespace StangaNetLib.Search.Extensions;

/// <summary>
/// Extension methods for registering StangaNetLib.Search services.
/// </summary>
/// <example>
/// Program.cs (in-memory backend, explicit index name):
/// <code>
/// builder.Services.AddStangaNetLibSearch&lt;ProductDocument&gt;("products");
/// builder.Services.AddStangaNetLibSearch&lt;OrderDocument&gt;("orders");
/// </code>
/// Program.cs (in-memory backend, index name from configuration):
/// <code>
/// // appsettings.json: { "Search": { "IndexName": "products" } }
/// builder.Services.AddStangaNetLibSearch&lt;ProductDocument&gt;(builder.Configuration);
/// </code>
/// Inject and use:
/// <code>
/// public class ProductService(ISearchService&lt;ProductDocument&gt; search)
/// {
///     public async Task&lt;SearchResult&lt;ProductDocument&gt;&gt; FindAsync(string term)
///     {
///         var result = await search.SearchAsync(new SearchQuery { Term = term });
///         return result.Value!;
///     }
/// }
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers StangaNetLib.Search with the <b>in-memory</b> backend for <typeparamref name="TDocument"/>
    /// using the specified <paramref name="indexName"/>.
    /// </summary>
    /// <typeparam name="TDocument">The document type to register a search service for.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="indexName">Name of the search index for this document type.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibSearch<TDocument>(
        this IServiceCollection services,
        string indexName)
        where TDocument : class
    {
        Guard.Against.NullOrWhiteSpace(indexName, nameof(indexName));
        return RegisterInMemory<TDocument>(services, indexName);
    }

    /// <summary>
    /// Registers StangaNetLib.Search with the <b>in-memory</b> backend for <typeparamref name="TDocument"/>,
    /// reading the index name from <paramref name="configSection"/> in <paramref name="configuration"/>.
    /// When <c>IndexName</c> is not configured or empty, falls back to
    /// <c>typeof(TDocument).Name.ToLowerInvariant()</c>.
    /// </summary>
    /// <typeparam name="TDocument">The document type to register a search service for.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="configSection">Configuration section to read <see cref="SearchSettings"/> from.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibSearch<TDocument>(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSection = SearchSettings.SectionName)
        where TDocument : class
    {
        Guard.Against.Null(configuration, nameof(configuration));
        Guard.Against.NullOrWhiteSpace(configSection, nameof(configSection));

        var settings = configuration.GetSection(configSection).Get<SearchSettings>() ?? new SearchSettings();
        var indexName = string.IsNullOrWhiteSpace(settings.IndexName)
            ? typeof(TDocument).Name.ToLowerInvariant()
            : settings.IndexName;

        return RegisterInMemory<TDocument>(services, indexName);
    }

    private static IServiceCollection RegisterInMemory<TDocument>(
        IServiceCollection services, string indexName)
        where TDocument : class
    {
        services.TryAddSingleton<InMemoryDocumentStore>();
        services.TryAddSingleton<ISearchIndexManager>(sp =>
            new InMemorySearchIndexManager(sp.GetRequiredService<InMemoryDocumentStore>()));
        services.AddSingleton<ISearchService<TDocument>>(sp =>
            new InMemorySearchService<TDocument>(sp.GetRequiredService<InMemoryDocumentStore>(), indexName));
        return services;
    }
}
