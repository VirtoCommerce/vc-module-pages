using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.Seo.Core.Models;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

public class PageDocumentSeoResolverTests
{
    private const string StoreId = "store";

    [Fact]
    public async Task FindSeoAsync_AsksForPagesOfTheVisitorOrganizationOnly()
    {
        PageDocumentSearchCriteria searched = null;
        var resolver = CreateResolver([], criteria => searched = criteria);

        await resolver.FindSeoAsync(new SeoSearchCriteria { StoreId = StoreId, Permalink = "about" });

        searched.Should().NotBeNull();
        searched.FilterByOrganization.Should().BeTrue();
        searched.OrganizationId.Should().BeNull();
        searched.Permalink.Should().Be("/about");
    }

    [Fact]
    public async Task FindSeoAsync_AnonymousVisitor_GetsPublicPagesForEveryUserGroupOnly()
    {
        var resolver = CreateResolver(AudiencePages());

        var result = await resolver.FindSeoAsync(new SeoSearchCriteria { StoreId = StoreId, Permalink = "/about" });

        result.Select(x => x.ObjectId).Should().Equal("everyone");
    }

    [Fact]
    public async Task FindSeoAsync_SignedInContact_GetsPagesForTheirUserGroupsAndRegisteredUsers()
    {
        var user = new ApplicationUser { Id = "user", MemberId = "contact" };
        var contact = new Contact { Id = "contact", Groups = ["vip"] };
        var resolver = CreateResolver(AudiencePages(), user: user, member: contact);

        var result = await resolver.FindSeoAsync(new SeoSearchCriteria { StoreId = StoreId, Permalink = "/about", UserId = "user" });

        result.Select(x => x.ObjectId).Should().Equal("vip", "registered", "everyone");
    }

    [Fact]
    public async Task FindSeoAsync_SignedInUserWithoutContact_IsTreatedAsAnonymous()
    {
        var user = new ApplicationUser { Id = "user", MemberId = "missing" };
        var resolver = CreateResolver(AudiencePages(), user: user);

        var result = await resolver.FindSeoAsync(new SeoSearchCriteria { StoreId = StoreId, Permalink = "/about", UserId = "user" });

        result.Select(x => x.ObjectId).Should().Equal("everyone");
    }

    [Fact]
    public async Task FindSeoAsync_OrdersByCreatedDateWhenNeverModifiedAndTreatsMissingUserGroupsAsEveryone()
    {
        var older = Page("older");
        older.ModifiedDate = null;
        older.CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = Page("newer");
        newer.ModifiedDate = null;
        newer.UserGroups = null;
        newer.CreatedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var resolver = CreateResolver([older, newer]);

        var result = await resolver.FindSeoAsync(new SeoSearchCriteria { StoreId = StoreId, Permalink = "/about" });

        result.Select(x => x.ObjectId).Should().Equal("newer", "older");
    }

    [Fact]
    public async Task FindSeoAsync_PutsTheMoreSpecificPageFirst()
    {
        var user = new ApplicationUser { Id = "user", MemberId = "contact" };
        var contact = new Contact { Id = "contact", Groups = ["vip"] };
        var resolver = CreateResolver(
        [
            Page("everyone-old", modified: 1),
            Page("everyone-new", modified: 2),
            Page("registered", visibility: PageDocumentVisibility.Private),
            Page("vip", userGroups: ["vip"]),
            Page("organization", organizationId: "org"),
        ], user: user, member: contact);

        var result = await resolver.FindSeoAsync(new SeoSearchCriteria { StoreId = StoreId, Permalink = "/about", UserId = "user", OrganizationId = "org" });

        result.Select(x => x.ObjectId).Should().Equal("organization", "vip", "registered", "everyone-new", "everyone-old");
    }

    [Fact]
    public async Task BuildRequestAsync_FilterByOrganizationWithoutOrganization_KeepsPagesForAnyOrganizationOnly()
    {
        var values = await GetOrganizationFilterValues(new PageDocumentSearchCriteria { FilterByOrganization = true });

        values.Should().Equal("__any");
    }

    [Fact]
    public async Task BuildRequestAsync_WithOrganization_KeepsPagesForThatOrAnyOrganization()
    {
        var values = await GetOrganizationFilterValues(new PageDocumentSearchCriteria { OrganizationId = "org" });

        values.Should().Equal("__any", "org");
    }

    [Fact]
    public async Task BuildRequestAsync_FilterByOrganizationWithOrganization_KeepsPagesForThatOrAnyOrganization()
    {
        var values = await GetOrganizationFilterValues(new PageDocumentSearchCriteria { FilterByOrganization = true, OrganizationId = "org" });

        values.Should().Equal("__any", "org");
    }

    [Fact]
    public async Task BuildRequestAsync_WithoutOrganizationOrFlag_DoesNotFilterByOrganization()
    {
        var values = await GetOrganizationFilterValues(new PageDocumentSearchCriteria());

        values.Should().BeNull();
    }

    private static PageDocument[] AudiencePages()
    {
        return
        [
            Page("everyone"),
            Page("registered", visibility: PageDocumentVisibility.Private),
            Page("vip", userGroups: ["vip"]),
            Page("gold", userGroups: ["gold"]),
        ];
    }

    private static PageDocument Page(
        string id,
        int modified = 0,
        PageDocumentVisibility visibility = PageDocumentVisibility.Public,
        string[] userGroups = null,
        string organizationId = null)
    {
        return new PageDocument
        {
            Id = id,
            StoreId = StoreId,
            Permalink = "/about",
            CultureName = "en-US",
            Status = PageDocumentStatus.Published,
            Visibility = visibility,
            UserGroups = userGroups ?? [],
            OrganizationId = organizationId,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(modified),
        };
    }

    private static PageDocumentSeoResolver CreateResolver(
        IList<PageDocument> pages,
        Action<PageDocumentSearchCriteria> onSearch = null,
        ApplicationUser user = null,
        Member member = null)
    {
        var searchService = new Mock<IPageDocumentSearchService>();
        searchService
            .Setup(x => x.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync((PageDocumentSearchCriteria criteria, bool _) =>
            {
                onSearch?.Invoke(criteria);
                return new PageDocumentSearchResult
                {
                    TotalCount = pages.Count,
                    Results = pages.Skip(criteria.Skip).Take(criteria.Take).ToList(),
                };
            });

        var store = new Store
        {
            Id = StoreId,
            Settings = [new ObjectSettingEntry { Name = ModuleConstants.Settings.General.Enable.Name, Value = true }],
        };
        var storeService = new Mock<IStoreService>();
        storeService
            .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([store]);

        var userManager = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        userManager.Setup(x => x.Users).Returns((user == null ? new List<ApplicationUser>() : [user]).AsQueryable());

        var memberService = new Mock<IMemberService>();
        memberService
            .Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string id, string _, string _) => member != null && member.Id == id ? member : null);

        return new PageDocumentSeoResolver(
            searchService.Object,
            () => userManager.Object,
            storeService.Object,
            memberService.Object);
    }

    private static async Task<IList<string>> GetOrganizationFilterValues(PageDocumentSearchCriteria criteria)
    {
        var builder = new PageSearchRequestBuilder(Mock.Of<ISearchPhraseParser>(), Mock.Of<IStoreService>());

        var request = await builder.BuildRequestAsync(criteria);

        return Flatten(request.Filter)
            .OfType<TermFilter>()
            .FirstOrDefault(x => x.FieldName == nameof(PageDocument.OrganizationId))
            ?.Values;
    }

    private static IEnumerable<IFilter> Flatten(IFilter filter)
    {
        return filter is AndFilter and
            ? and.ChildFilters.SelectMany(Flatten)
            : [filter];
    }
}
