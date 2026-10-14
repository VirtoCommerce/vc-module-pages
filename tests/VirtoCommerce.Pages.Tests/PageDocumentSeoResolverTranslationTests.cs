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
        Assert.DoesNotContain(search.Requests, x => x.TranslationGroupId != null);
    }

    private static SeoSearchCriteria Criteria(string permalink, string languageCode)
    {
        return new SeoSearchCriteria { StoreId = StoreId, Permalink = permalink, LanguageCode = languageCode, Take = 20 };
    }

    private static PageDocument Page(string id, string cultureName, string permalink, string translationGroupId)
    {
        return new PageDocument
        {
            Id = id,
            StoreId = StoreId,
            CultureName = cultureName,
            Permalink = permalink,
            TranslationGroupId = translationGroupId,
            Status = PageDocumentStatus.Published,
            CreatedDate = DateTime.UtcNow,
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
                .Where(x => criteria.TranslationGroupId == null || x.TranslationGroupId == criteria.TranslationGroupId)
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
