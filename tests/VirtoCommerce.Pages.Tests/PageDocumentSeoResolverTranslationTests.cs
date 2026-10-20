using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.Seo.Core.Models;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

public class PageDocumentSeoResolverTranslationTests
{
    private const string StoreId = "store";
    private const string GroupId = "faq";

    // Bossard: Dutch and English versions of the same page with their own permalinks.
    private static readonly PageDocument[] Documents =
    [
        Page("nl", "nl-NL", "/veelgestelde-vragen", GroupId),
        Page("en", "en-US", "/faq", GroupId),
        Page("about-en", "en-US", "/about-us", null),
    ];

    [Fact]
    public async Task FindSeoAsync_OtherLanguagePermalink_ReturnsRequestedLanguageVersion()
    {
        var result = await CreateResolver().FindSeoAsync(Criteria("veelgestelde-vragen", "en-US"));

        var info = Assert.Single(result);
        Assert.Equal(("en-US", "/faq"), (info.LanguageCode, info.SemanticUrl));
    }

    [Fact]
    public async Task FindSeoAsync_DefaultLanguagePermalink_AddsRequestedLanguageVersion()
    {
        // The default language version matches the permalink too, x-api then prefers the exact language.
        var result = await CreateResolver().FindSeoAsync(Criteria("faq", "nl-NL"));

        Assert.Equal(["en-US:/faq", "nl-NL:/veelgestelde-vragen"], result.Select(x => $"{x.LanguageCode}:{x.SemanticUrl}"));
    }

    [Fact]
    public async Task FindSeoAsync_NoTranslation_DefaultLanguageBehavior_ReturnsDefaultLanguageVersion()
    {
        var result = await CreateResolver().FindSeoAsync(Criteria("veelgestelde-vragen", "de-DE"));

        var info = Assert.Single(result);
        Assert.Equal(("en-US", "/faq"), (info.LanguageCode, info.SemanticUrl));
    }

    [Fact]
    public async Task FindSeoAsync_NoTranslation_NotFoundBehavior_ReturnsNothing()
    {
        var resolver = CreateResolver(ModuleConstants.MissingTranslationBehaviors.NotFound);

        Assert.Empty(await resolver.FindSeoAsync(Criteria("veelgestelde-vragen", "de-DE")));
        Assert.Empty(await resolver.FindSeoAsync(Criteria("faq", "de-DE")));
    }

    [Fact]
    public async Task FindSeoAsync_PageWithoutTranslations_Unchanged()
    {
        var result = await CreateResolver(ModuleConstants.MissingTranslationBehaviors.NotFound)
            .FindSeoAsync(Criteria("about-us", "de-DE"));

        var info = Assert.Single(result);
        Assert.Equal("/about-us", info.SemanticUrl);
    }

    [Fact]
    public async Task FindSeoAsync_ExactLanguageMatch_DoesNotLookForTranslations()
    {
        var search = new FakePageDocumentSearchService(Documents);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("faq", "en-US"));

        Assert.Equal(["/faq"], result.Select(x => x.SemanticUrl));
        var request = Assert.Single(search.Requests);
        Assert.Null(request.TranslationGroupIds);
    }

    [Fact]
    public async Task FindSeoAsync_NotAPage_DoesNotLookForTranslations()
    {
        // Every slug (products, categories) goes through the page resolver first: the language search and the search in
        // every language, never a translation search.
        var search = new FakePageDocumentSearchService(Documents);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("some-product", "nl-NL"));

        Assert.Empty(result);
        Assert.Equal(["nl-NL", null], search.Requests.Select(x => x.LanguageCode));
        Assert.All(search.Requests, x => Assert.Null(x.TranslationGroupIds));
    }

    [Fact]
    public async Task FindSeoAsync_LanguageFilter_GoesThroughSearchCriteria()
    {
        // The language filter is applied by PageSearchRequestBuilder.GetFilters, so its overrides keep working.
        var search = new FakePageDocumentSearchService(Documents);

        await CreateResolver(search: search).FindSeoAsync(Criteria("faq", "nl-NL"));

        var first = search.Requests[0];
        Assert.Equal(("/faq", "nl-NL"), (first.Permalink, first.LanguageCode));
        Assert.DoesNotContain(search.Requests, x => x.Permalink != null && x.LanguageCode == null);
    }

    [Fact]
    public async Task FindSeoAsync_NothingInRequestedLanguage_SearchesPermalinkInEveryLanguage()
    {
        var search = new FakePageDocumentSearchService(Documents);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("veelgestelde-vragen", "en-US"));

        Assert.Equal(["/faq"], result.Select(x => x.SemanticUrl));
        Assert.Equal(3, search.Requests.Count);
        Assert.Equal(("/veelgestelde-vragen", "en-US"), (search.Requests[0].Permalink, search.Requests[0].LanguageCode));
        Assert.Equal(("/veelgestelde-vragen", null), (search.Requests[1].Permalink, search.Requests[1].LanguageCode));
        Assert.Equal([GroupId], search.Requests[2].TranslationGroupIds);
    }

    [Fact]
    public async Task FindSeoAsync_SeveralTranslationGroups_SearchesTranslationsOnce()
    {
        // The same permalink is used by two different pages in other languages.
        var search = new FakePageDocumentSearchService(
        [
            Page("news-nl", "nl-NL", "/nieuws", "news"),
            Page("blog-fr", "fr-FR", "/nieuws", "blog"),
            Page("news-de", "de-DE", "/nachrichten", "news"),
        ]);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("nieuws", "de-DE"));

        var info = Assert.Single(result);
        Assert.Equal(("de-DE", "/nachrichten"), (info.LanguageCode, info.SemanticUrl));
        var request = Assert.Single(search.Requests, x => x.TranslationGroupIds != null);
        Assert.Equal(["blog", "news"], request.TranslationGroupIds.Order());
    }

    [Fact]
    public async Task FindSeoAsync_SeveralTranslationsInOneLanguage_ReturnsLatestModified()
    {
        var older = Page("faq-nl-1", "nl-NL", "/veelgestelde-vragen", GroupId);
        older.ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = Page("faq-nl-2", "nl-NL", "/vragen", GroupId);
        newer.ModifiedDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var search = new FakePageDocumentSearchService([older, newer, Page("en", "en-US", "/faq", GroupId)]);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("faq", "nl-NL"));

        Assert.Equal(["en-US:/faq", "nl-NL:/vragen"], result.Select(x => $"{x.LanguageCode}:{x.SemanticUrl}"));
    }

    [Fact]
    public async Task FindSeoAsync_SeveralTranslationsInOneLanguageWithSameDate_ReturnsFirstById()
    {
        var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var search = new FakePageDocumentSearchService(
        [
            Page("faq-nl-b", "nl-NL", "/vragen-b", GroupId, date),
            Page("faq-nl-a", "nl-NL", "/vragen-a", GroupId, date),
            Page("en", "en-US", "/faq", GroupId),
        ]);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("faq", "nl-NL"));

        Assert.Equal(["en-US:/faq", "nl-NL:/vragen-a"], result.Select(x => $"{x.LanguageCode}:{x.SemanticUrl}"));
    }

    [Fact]
    public async Task FindSeoAsync_NoTranslationAndNoDefaultLanguageVersion_ReturnsMatchedVersion()
    {
        var search = new FakePageDocumentSearchService(
        [
            Page("contact-nl", "nl-NL", "/contact-opnemen", "contact"),
            Page("contact-fr", "fr-FR", "/nous-contacter", "contact"),
        ]);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("contact-opnemen", "de-DE"));

        var info = Assert.Single(result);
        Assert.Equal(("nl-NL", "/contact-opnemen"), (info.LanguageCode, info.SemanticUrl));
    }

    private static SeoSearchCriteria Criteria(string permalink, string languageCode)
    {
        return new SeoSearchCriteria { StoreId = StoreId, Permalink = permalink, LanguageCode = languageCode, Take = 20 };
    }

    private static PageDocument Page(string id, string cultureName, string permalink, string translationGroupId,
        DateTime? createdDate = null)
    {
        return new PageDocument
        {
            Id = id,
            StoreId = StoreId,
            CultureName = cultureName,
            Permalink = permalink,
            TranslationGroupId = translationGroupId,
            Status = PageDocumentStatus.Published,
            CreatedDate = createdDate ?? DateTime.UtcNow,
        };
    }

    private static PageDocumentSeoResolver CreateResolver(
        string missingTranslationBehavior = ModuleConstants.MissingTranslationBehaviors.DefaultLanguage,
        FakePageDocumentSearchService search = null)
    {
        var store = new Store
        {
            Id = StoreId,
            DefaultLanguage = "en-US",
            Settings =
            [
                new ObjectSettingEntry { Name = ModuleConstants.Settings.General.Enable.Name, Value = true },
                new ObjectSettingEntry
                {
                    Name = ModuleConstants.Settings.General.MissingTranslationBehavior.Name,
                    Value = missingTranslationBehavior,
                },
            ],
        };
        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([store]);

        var userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        userManager.Setup(x => x.Users).Returns(Enumerable.Empty<ApplicationUser>().AsQueryable());

        return new PageDocumentSeoResolver(
            search ?? new FakePageDocumentSearchService(Documents),
            () => userManager.Object,
            storeService.Object,
            Mock.Of<IMemberService>());
    }

    /// <summary>Filters like the index: by permalink, translation group and the language filter (language, store default, any).</summary>
    private sealed class FakePageDocumentSearchService(IList<PageDocument> documents) : IPageDocumentSearchService
    {
        public List<PageDocumentSearchCriteria> Requests { get; } = [];

        public Task<PageDocumentSearchResult> SearchAsync(PageDocumentSearchCriteria criteria, bool clone = true)
        {
            Requests.Add(criteria);

            var results = documents
                .Where(x => criteria.Permalink == null || x.Permalink == criteria.Permalink)
                .Where(x => criteria.TranslationGroupIds == null || criteria.TranslationGroupIds.Contains(x.TranslationGroupId))
                .Where(x => criteria.LanguageCode == null || x.CultureName == criteria.LanguageCode || x.CultureName == "en-US")
                .ToList();

            return Task.FromResult(new PageDocumentSearchResult
            {
                TotalCount = results.Count,
                Results = criteria.Skip == 0 ? results : [],
            });
        }

        public Task<IndexingResult> IndexDocuments(PageDocument[] documents) => throw new NotSupportedException();

        public Task<IndexingResult> RemoveDocuments(string[] documentIds) => throw new NotSupportedException();
    }
}
