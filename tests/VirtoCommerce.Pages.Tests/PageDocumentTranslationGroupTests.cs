using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Converters;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

public class PageDocumentTranslationGroupTests
{
    [Fact]
    public void ToIndexDocument_WithTranslationGroup_IndexesFilterableField()
    {
        var document = new PageDocumentConverter().ToIndexDocument(CreatePage("group-1"));

        var field = document.Fields.Single(x => x.Name == nameof(PageDocument.TranslationGroupId));
        Assert.True(field.IsFilterable);
        Assert.Equal("group-1", field.Value);
    }

    [Fact]
    public void ToIndexDocument_TranslationGroup_IsNotSearchableText()
    {
        var document = new PageDocumentConverter().ToIndexDocument(CreatePage("group-1"));

        Assert.DoesNotContain(document.Fields, x => x.Name != nameof(PageDocument.TranslationGroupId) &&
            (Equals(x.Value, "group-1") || x.Values?.Contains("group-1") == true));
    }

    [Fact]
    public void ToIndexDocument_WithoutTranslationGroup_DoesNotIndexField()
    {
        var document = new PageDocumentConverter().ToIndexDocument(CreatePage(null));

        Assert.DoesNotContain(document.Fields, x => x.Name == nameof(PageDocument.TranslationGroupId));
    }

    [Fact]
    public void ToPageDocument_ReadsTranslationGroup()
    {
        var searchDocument = new SearchDocument
        {
            ["status"] = nameof(PageDocumentStatus.Published),
            ["visibility"] = nameof(PageDocumentVisibility.Public),
            ["usergroups"] = PageDocumentConverter.Any,
            ["translationgroupid"] = "group-1",
        };

        var page = new PageDocumentConverter().ToPageDocument(searchDocument);

        Assert.Equal("group-1", page.TranslationGroupId);
    }

    [Fact]
    public async Task BuildRequestAsync_WithTranslationGroup_AddsTermFilter()
    {
        var builder = new PageSearchRequestBuilder(new SearchPhraseParser(new NullLogger<SearchPhraseParser>()), Mock.Of<IStoreService>());
        var criteria = new PageDocumentSearchCriteria { TranslationGroupId = "group-1", Visibility = PageDocumentVisibility.Private };

        var request = await builder.BuildRequestAsync(criteria);

        // A single filter is not wrapped into an AndFilter.
        var filters = request.Filter is AndFilter and ? and.ChildFilters : [request.Filter];
        var term = filters.OfType<TermFilter>().Single(x => x.FieldName == nameof(PageDocument.TranslationGroupId));
        Assert.Equal(["group-1"], term.Values);
    }

    private static PageDocument CreatePage(string translationGroupId)
    {
        return new PageDocument
        {
            Id = "page-1",
            StoreId = "store",
            CultureName = "nl-NL",
            Permalink = "/over-ons",
            CreatedDate = DateTime.UtcNow,
            TranslationGroupId = translationGroupId,
        };
    }
}
