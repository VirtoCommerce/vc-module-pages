using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.ElasticSearchModule.Data;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.Pages.Tests;

public class ElasticSearchTests(IConfiguration configuration) : SearchProviderTests
{
    protected override ISearchProvider GetSearchProvider()
    {
        var searchOptions = Options.Create(new SearchOptions { Scope = "test-core", Provider = "ElasticSearch" });
        var elasticOptions = Options.Create(configuration.GetSection("ElasticSearch").Get<ElasticSearchOptions>());
        elasticOptions.Value.Server ??= Environment.GetEnvironmentVariable("TestElasticsearchHost") ?? "localhost:9200";
        var connectionSettings = new ElasticSearchConnectionSettings(elasticOptions);
        var client = new ElasticSearchClient(connectionSettings);
        var loggerFactory = LoggerFactory.Create(builder => { builder.ClearProviders(); });
        var logger = loggerFactory.CreateLogger<ElasticSearchProvider>();

        var provider = new ElasticSearchProvider(searchOptions, GetSettingsManager(), client, new ElasticSearchRequestBuilder(), logger);

        return provider;
    }
}
