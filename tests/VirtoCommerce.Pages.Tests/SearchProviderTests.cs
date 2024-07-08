using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
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
    public virtual async Task CanAddAndRemoveDocuments()
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

        //// Update index with new fields and add more documents
        //var secondaryDocuments = GetSecondaryDocuments();

        //response = await provider.IndexAsync(DocumentType, secondaryDocuments);

        //Assert.NotNull(response);
        //Assert.NotNull(response.Items);
        //Assert.Equal(secondaryDocuments.Count, response.Items.Count);
        //Assert.All(response.Items, i => Assert.True(i.Succeeded));

        //// Remove some documents
        //response = await provider.RemoveAsync(DocumentType,
        //    new[] { new IndexDocument("Item-7"), new IndexDocument("Item-8") });

        //Assert.NotNull(response);
        //Assert.NotNull(response.Items);
        //Assert.Equal(2, response.Items.Count);
        //Assert.All(response.Items, i => Assert.True(i.Succeeded));
    }

    private PageDocumentSearchService GetSearchService(ISearchProvider provider)
    {
        var logger = new NullLogger<SearchPhraseParser>();
        var parser = new SearchPhraseParser(logger);
        var builder = new PagesSearchRequestBuilder(parser);

        return new PageDocumentSearchService(provider, builder);
    }

}
