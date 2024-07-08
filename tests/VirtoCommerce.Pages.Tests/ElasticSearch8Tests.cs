using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VirtoCommerce.ElasticSearch8.Core.Models;
using VirtoCommerce.ElasticSearch8.Data.Services;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.Pages.Tests;

public class ElasticSearch8Tests(IConfiguration configuration) : SearchProviderTests
{
    protected override ISearchProvider GetSearchProvider()
    {
        var searchOptions = Options.Create(new SearchOptions { Scope = "test-core", Provider = "ElasticSearch8" });
        var elasticOptions = Options.Create(configuration.GetSection("ElasticSearch8").Get<ElasticSearch8Options>());
        elasticOptions.Value.Server ??= Environment.GetEnvironmentVariable("TestElasticsearchHost") ?? "localhost:9200";

        var settingsManager = GetSettingsManager();

        var filtersBuilder = new ElasticSearchFiltersBuilder();
        var aggregationsBuilder = new ElasticSearchAggregationsBuilder(filtersBuilder);
        var requestBuilder = new ElasticSearchRequestBuilder(filtersBuilder, aggregationsBuilder, settingsManager);

        var responseBuilder = new ElasticSearchResponseBuilder();
        var propertyService = new ElasticSearchPropertyService();

        var loggerFactory = LoggerFactory.Create(builder => { builder.ClearProviders(); });
        var logger = loggerFactory.CreateLogger<ElasticSearch8Provider>();

        var provider = new ElasticSearch8Provider(
            searchOptions,
            elasticOptions,
            settingsManager,
            requestBuilder,
            responseBuilder,
            propertyService,
            logger
        );

        return provider;
    }
}
