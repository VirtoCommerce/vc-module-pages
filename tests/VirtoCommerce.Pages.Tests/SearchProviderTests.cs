using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Converters;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.SearchPhraseParsing;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
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
        var pages = GetPagesSet1();

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
        var pages = GetPagesSet1();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        // Search
        var searchCriteria = new PageDocumentSearchCriteria
        {
            StoreId = "store1",
            Visibility = PageDocumentVisibility.Public,
            Status = PageDocumentStatus.Published,
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
        var pages = GetPagesSet1();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        var removeResult = await service.RemoveDocuments([pages[1].Id]);

        Assert.All(removeResult.Items, i => Assert.True((bool)i.Succeeded));

        var searchCriteria = new PageDocumentSearchCriteria
        {
            StoreId = "store1",
            Visibility = PageDocumentVisibility.Public,
            Status = PageDocumentStatus.Published,
            Skip = 0,
            Take = 10
        };

        var result = await service.SearchAsync(searchCriteria);

        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Equal(pages.Count(x => x.StoreId == searchCriteria.StoreId) - 1, result.Results.Count);
    }

    [Fact]
    public virtual async Task SearchFiltersAreCorrect()
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPagesSet1();

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
        var pages = GetPagesSet1();

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

    [Theory]
    [InlineData("store1", "/test-page", PageDocumentVisibility.Public, PageDocumentStatus.Published, null, null, 1)]
    [InlineData("store1", "/test-page", PageDocumentVisibility.Public, PageDocumentStatus.Published, 12, null, 1)]
    [InlineData("store1", "/test-page", PageDocumentVisibility.Public, PageDocumentStatus.Published, 10, null, 2)]
    [InlineData("store1", "/test-page", PageDocumentVisibility.Public, PageDocumentStatus.Published, 11, null, 2)]
    [InlineData("store1", "/test-page", PageDocumentVisibility.Public, PageDocumentStatus.Draft, null, null, 1)]
    [InlineData("store1", "/test-page", PageDocumentVisibility.Public, PageDocumentStatus.Published, null, "admin", 2)]
    [InlineData("store2", "/testpage", PageDocumentVisibility.Private, PageDocumentStatus.Unpublished, null, "admin", 1)]
    public virtual async Task SearchPages(string storeId, string permalink,
        PageDocumentVisibility visibility, PageDocumentStatus status,
        int? certainDay, string userGroupsAsString,
        int expectedCount)
    {
        var provider = GetSearchProvider();
        var service = GetSearchService(provider);

        // Delete index
        await provider.DeleteIndexAsync(DocumentType);

        // Create index and add pages
        var pages = GetPagesSet2();

        var response = await service.IndexDocuments(pages.ToArray());

        Assert.NotNull(response);
        Assert.NotNull(response.Items);
        Assert.Equal(pages.Count, response.Items.Count);
        Assert.All(response.Items, i => Assert.True(i.Succeeded));

        var certainDate = certainDay.HasValue ? new DateTime(2024, 7, certainDay.Value) : (DateTime?)null;
        var userGroups = userGroupsAsString.IsNullOrEmpty() ? [] : userGroupsAsString.Split(",");

        var searchCriteria = new PageDocumentSearchCriteria
        {
            Permalink = permalink,
            StoreId = storeId,
            Visibility = visibility,
            Status = status,
            CertainDate = certainDate,
            UserGroups = userGroups,
            Skip = 0,
            Take = 10
        };

        var result = await service.SearchAsync(searchCriteria);

        Assert.NotNull(result);
        Assert.NotNull(result.Results);
        Assert.Equal(expectedCount, result.Results.Count);
        if (result.Results.Any())
        {
            Assert.All(result.Results, i => Assert.Equal(storeId, i.StoreId));
            Assert.All(result.Results, i => Assert.Equal(permalink, i.Permalink));
            Assert.All(result.Results, i => Assert.Equal(visibility, i.Visibility));
            Assert.All(result.Results, i => Assert.Equal(status, i.Status));
            Assert.All(result.Results, i => Assert.True(i.StartDate == null || i.StartDate <= (certainDate ?? DateTime.UtcNow)));
            Assert.All(result.Results, i => Assert.True(i.EndDate == null || i.EndDate >= (certainDate ?? DateTime.UtcNow)));
            Assert.All(result.Results, i =>
            {
                var contains = i.UserGroups.Length == 0 || i.UserGroups.Any(userGroups.Contains);
#pragma warning disable xUnit2012
                Assert.True(contains);
#pragma warning restore xUnit2012
            });
        }
    }

    private PageDocumentSearchService GetSearchService(ISearchProvider provider)
    {
        var logger = new NullLogger<SearchPhraseParser>();
        var storeService = GetStoreService();
        var parser = new SearchPhraseParser(logger);
        var builder = new PageSearchRequestBuilder(parser, storeService);
        var converter = new PageDocumentConverter();

        return new PageDocumentSearchService(provider, builder, converter);
    }

    private IStoreService GetStoreService()
    {
        var storeService = new Mock<IStoreService>();

        storeService.Setup(x => x.GetAsync(It.IsAny<string[]>(), null, true))
            .ReturnsAsync((string[] ids, string _, bool _) => new List<Store> {
                new ()
                {
                    Id = ids[0],
                    Name = "Test store",
                    Url = "http://localhost",
                    TimeZone = "UTC",
                    Languages = new[] { "en-US" }
                }
            });

        return storeService.Object;
    }

}
