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
    public const string DocumentType = "item";

    [Fact]
    public virtual async Task CanAddDocuments()
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPages();

        var response = await service.IndexDocument(pages.ToArray());

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

        var response = await service.IndexDocument(pages.ToArray());

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

        var response = await service.IndexDocument(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        // todo: remove documents


    }

    private PageDocumentSearchService GetSearchService(ISearchProvider provider)
    {
        var logger = new NullLogger<SearchPhraseParser>();
        var parser = new SearchPhraseParser(logger);
        var builder = new PagesSearchRequestBuilder(parser);

        return new PageDocumentSearchService(provider, builder);
    }

}
