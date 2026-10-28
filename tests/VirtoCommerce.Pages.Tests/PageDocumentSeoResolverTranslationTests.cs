using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.Seo.Core.Extensions;
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
    public async Task FindSeoAsync_NotAPage_SearchesTheIndexOnce()
    {
        // Every slug (products, categories) goes through the page resolver first.
        var search = new FakePageDocumentSearchService(Documents);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("some-product", "nl-NL"));

        Assert.Empty(result);
        Assert.Single(search.Requests);
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
        Assert.Equal(("contact-nl", "/contact-opnemen"), (info.ObjectId, info.SemanticUrl));
        // Neither the requested nor the store default language: reported for any language, so x-api does not drop it.
        Assert.Null(info.LanguageCode);
        Assert.Same(info, result.GetBestMatchingSeoInfo(StoreId, "en-US", "de-DE"));
    }

    [Fact]
    public async Task FindSeoAsync_AnonymousAndTranslationForAGroup_ReturnsDefaultLanguageVersion()
    {
        var restricted = Page("nl", "nl-NL", "/veelgestelde-vragen", GroupId);
        restricted.UserGroups = ["vip"];
        var search = new FakePageDocumentSearchService([restricted, Page("en", "en-US", "/faq", GroupId)]);

        var ownPermalink = await CreateResolver(search: search).FindSeoAsync(Criteria("veelgestelde-vragen", "nl-NL"));
        var defaultPermalink = await CreateResolver(search: search).FindSeoAsync(Criteria("faq", "nl-NL"));

        Assert.Equal(["en-US:/faq"], ownPermalink.Select(x => $"{x.LanguageCode}:{x.SemanticUrl}"));
        Assert.Equal(["en-US:/faq"], defaultPermalink.Select(x => $"{x.LanguageCode}:{x.SemanticUrl}"));
    }

    [Fact]
    public async Task FindSeoAsync_AnonymousAndPrivateTranslation_ReturnsDefaultLanguageVersion()
    {
        var restricted = Page("nl", "nl-NL", "/veelgestelde-vragen", GroupId);
        restricted.Visibility = PageDocumentVisibility.Private;
        var search = new FakePageDocumentSearchService([restricted, Page("en", "en-US", "/faq", GroupId)]);

        var result = await CreateResolver(search: search).FindSeoAsync(Criteria("veelgestelde-vragen", "nl-NL"));

        Assert.Equal(["en-US:/faq"], result.Select(x => $"{x.LanguageCode}:{x.SemanticUrl}"));
    }

    [Fact]
    public async Task FindSeoAsync_AnonymousAndPageForAGroup_ReturnsNothing()
    {
        var restricted = Page("about-en", "en-US", "/about-us", null);
        restricted.UserGroups = ["vip"];
        var search = new FakePageDocumentSearchService([restricted]);

        Assert.Empty(await CreateResolver(search: search).FindSeoAsync(Criteria("about-us", "en-US")));
    }

    [Fact]
    public async Task FindSeoAsync_MemberOfTheGroup_ReturnsTranslationForTheGroup()
    {
        var restricted = Page("nl", "nl-NL", "/veelgestelde-vragen", GroupId);
        restricted.UserGroups = ["vip"];
        var search = new FakePageDocumentSearchService([restricted, Page("en", "en-US", "/faq", GroupId)]);
        var member = new Contact { Id = "member", Groups = ["vip"] };

        var result = await CreateResolver(search: search, member: member).FindSeoAsync(Criteria("faq", "nl-NL", userId: "user"));

        Assert.Equal(["en-US:/faq", "nl-NL:/veelgestelde-vragen"], result.Select(x => $"{x.LanguageCode}:{x.SemanticUrl}"));
    }

    [Fact]
    public async Task FindSeoAsync_LanguageCodeInOtherCase_ReturnsRequestedLanguageVersion()
    {
        var result = await CreateResolver().FindSeoAsync(Criteria("veelgestelde-vragen", "en-us"));

        var info = Assert.Single(result);
        Assert.Equal(("en-US", "/faq"), (info.LanguageCode, info.SemanticUrl));
    }

    [Fact]
    public async Task FindSeoAsync_NotFoundBehaviorAndSeveralGroups_KeepsOnlyPagesWithoutTranslations()
    {
        var search = new FakePageDocumentSearchService(
        [
            Page("info-a", "en-US", "/info", "a"),
            Page("info-b", "nl-NL", "/info", "b"),
            Page("info-any", null, "/info", null),
        ]);

        var result = await CreateResolver(ModuleConstants.MissingTranslationBehaviors.NotFound, search)
            .FindSeoAsync(Criteria("info", "de-DE"));

        var info = Assert.Single(result);
        Assert.Equal("info-any", info.ObjectId);
    }

    private static SeoSearchCriteria Criteria(string permalink, string languageCode, string userId = null)
    {
        return new SeoSearchCriteria { StoreId = StoreId, Permalink = permalink, LanguageCode = languageCode, UserId = userId, Take = 20 };
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
            Visibility = PageDocumentVisibility.Public,
            CreatedDate = createdDate ?? DateTime.UtcNow,
        };
    }

    private static PageDocumentSeoResolver CreateResolver(
        string missingTranslationBehavior = ModuleConstants.MissingTranslationBehaviors.DefaultLanguage,
        FakePageDocumentSearchService search = null,
        Member member = null)
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
        ApplicationUser[] users = member == null ? [] : [new ApplicationUser { Id = "user", MemberId = member.Id }];
        userManager.Setup(x => x.Users).Returns(users.AsQueryable());

        var memberService = new Mock<IMemberService>();
        memberService.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(member);

        return new PageDocumentSeoResolver(
            search ?? new FakePageDocumentSearchService(Documents),
            () => userManager.Object,
            storeService.Object,
            memberService.Object);
    }

    /// <summary>
    /// Filters like the index: by permalink, translation group, the language filter (language, store default, any)
    /// user groups (a page without groups is for any group; null criteria groups skip the filter) and visibility.
    /// </summary>
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
                .Where(x => criteria.UserGroups == null || x.UserGroups == null || x.UserGroups.Length == 0 ||
                            x.UserGroups.Intersect(criteria.UserGroups).Any())
                .Where(x => criteria.Visibility != PageDocumentVisibility.Public || x.Visibility == PageDocumentVisibility.Public)
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
