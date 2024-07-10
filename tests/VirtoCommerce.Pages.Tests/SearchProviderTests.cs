using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

[Trait("Category", "IntegrationTest")]
public abstract class SearchProviderTests : SearchProviderTestsBase
{
    public const string DocumentType = "Pages";

    [Fact]
    public virtual async Task CanAddDocuments()
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPages();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));
    }

    [Fact]
    public virtual async Task CanSearchDocuments()
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPages();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        // Search
        var searchCriteria = new PageDocumentSearchCriteria
        {
            SearchPhrase = "page",
            Skip = 0,
            Take = 10
        };

        var searchResponse = await service.SearchAsync(searchCriteria);

        Assert.NotNull(searchResponse);
        Assert.NotNull(searchResponse.Results);
        Assert.NotEmpty(searchResponse.Results);
        // Assert.All(searchResponse.Results, r => Assert.Equal(DocumentType, r.));
    }

    [Fact]
    public virtual async Task CanRemoveDocuments()
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPages();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        var removeResult = await service.RemoveDocuments([pages[1].Id]);

        Assert.All(removeResult.Items, i => Assert.True((bool)i.Succeeded));

        var searchCriteria = new PageDocumentSearchCriteria
        {
            Skip = 0,
            Take = 10
        };

        var result = await service.SearchAsync(searchCriteria);

        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Equal(pages.Count - 1, result.Results.Count);
    }

    [Fact]
    public virtual async Task SearchFiltersAreCorrect()
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPages();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        var searchCriteria = new PageDocumentSearchCriteria
        {
            Permalink = "/test-page",
            StoreId = "store1",
            Skip = 0,
            Take = 10
        };

        var result = await service.SearchAsync(searchCriteria);

        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, i => Assert.Equal("/test-page", i.Permalink));
        Assert.All(result.Results, i => Assert.Equal("store1", i.StoreId));
    }

    [Fact]
    public virtual async Task SearchReturnsCorrectDocument()
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPages();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        var searchCriteria = new PageDocumentSearchCriteria
        {
            Permalink = "/test-page",
            StoreId = "store1",
            Visibility = PageDocumentVisibility.Public,
            Status = PageDocumentStatus.Published,
            Skip = 0,
            Take = 10
        };

        var results = await service.SearchAsync(searchCriteria);

        Assert.NotNull(results);
        Assert.NotNull(results.Results);
        Assert.Single(results.Results);

        var original = pages.First();
        var result = results.Results[0];

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.OuterId, result.OuterId);
        Assert.Equal(original.StoreId, result.StoreId);
        Assert.Equal(original.CultureName, result.CultureName);
        Assert.Equal(original.Permalink, result.Permalink);
        Assert.Equal(original.Title, result.Title);
        Assert.Equal(original.Description, result.Description);
        Assert.Equal(original.Status, result.Status);

        Assert.Equal(original.CreatedDate, result.CreatedDate);
        Assert.Equal(original.ModifiedDate, result.ModifiedDate);
        Assert.Equal(original.CreatedBy, result.CreatedBy);
        Assert.Equal(original.ModifiedBy, result.ModifiedBy);

        Assert.Equal(original.Source, result.Source);
        Assert.Equal(original.MimeType, result.MimeType);
        Assert.Equal(original.Content, result.Content);
        Assert.Equal(original.Visibility, result.Visibility);

        Assert.Equal(original.UserGroups, result.UserGroups);
        Assert.Equal(original.StartDate, result.StartDate);
        Assert.Equal(original.EndDate, result.EndDate);
    }

    private PageDocumentSearchService GetSearchService(ISearchProvider provider)
    {
        var logger = new NullLogger<SearchPhraseParser>();
        var parser = new SearchPhraseParser(logger);
        var builder = new PagesSearchRequestBuilder(parser);

        return new PageDocumentSearchService(provider, builder);
    }

}
