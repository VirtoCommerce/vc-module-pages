using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.Seo.Core.Models;
using VirtoCommerce.Seo.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search;

public class PageDocumentSeoResolver(IPageDocumentSearchService searchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IStoreService storeService,
    IMemberService memberService)
    : ISeoResolver
{
    public async Task<IList<SeoInfo>> FindSeoAsync(SeoSearchCriteria criteria)
    {
        var store = await storeService.GetByIdAsync(criteria.StoreId);
        if (store == null)
        {
            throw new InvalidOperationException($"Store with ID {criteria.StoreId} not found");
        }
        var pagesEnabled = store.Settings.GetValue<bool>(ModuleConstants.Settings.General.Enable);
        if (pagesEnabled)
        {
            return OrderBySpecificity((await FindFiles(criteria)).DistinctBy(x => x.Id))
                .Select(x =>
                {
                    var info = AbstractTypeFactory<SeoInfo>.TryCreateInstance();
                    info.Name = x.Title;
                    info.SemanticUrl = x.Permalink;
                    info.StoreId = x.StoreId;
                    info.LanguageCode = x.CultureName;
                    info.ObjectId = x.Id;
                    info.Id = x.Id;
                    info.OrganizationId = x.OrganizationId;
                    // the FindFiles method always returns only active documents
                    info.IsActive = true;
                    info.ObjectType = ModuleConstants.PageDocumentType;
                    info.CreatedBy = x.CreatedBy;
                    info.CreatedDate = x.CreatedDate;
                    return info;
                })
                .ToList();
        }

        return [];
    }

    // Several published pages can share a permalink, for different audiences or periods. The SEO module picks one
    // by store, organization and language and keeps this order among equal candidates, so the more specific page
    // goes first: one for the visitor's organization, then one for their user groups, then one for registered
    // users only; on a tie, the most recently changed page.
    protected virtual IEnumerable<PageDocument> OrderBySpecificity(IEnumerable<PageDocument> pages)
    {
        return pages
            .OrderBy(x => x.CultureName)
            .ThenByDescending(x => !x.OrganizationId.IsNullOrEmpty())
            .ThenByDescending(x => x.UserGroups is { Length: > 0 })
            .ThenByDescending(x => x.Visibility == PageDocumentVisibility.Private)
            .ThenByDescending(x => x.ModifiedDate ?? x.CreatedDate)
            .ThenBy(x => x.Id, StringComparer.Ordinal);
    }

    private async Task<IList<PageDocument>> FindFiles(SeoSearchCriteria criteria)
    {
        var permalink = criteria.Permalink?.StartsWith('/') ?? false
            ? criteria.Permalink
            : $"/{criteria.Permalink}";

        var searchCriteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        searchCriteria.StoreId = criteria.StoreId;
        searchCriteria.LanguageCode = criteria.LanguageCode;
        searchCriteria.Permalink = permalink;
        searchCriteria.Skip = criteria.Skip;
        searchCriteria.Take = criteria.Take;
        searchCriteria.CertainDate = DateTime.UtcNow;
        searchCriteria.Status = PageDocumentStatus.Published;
        searchCriteria.OrganizationId = criteria.OrganizationId;
        // A visitor without an organization must not get pages made for one.
        searchCriteria.FilterByOrganization = true;

        var member = await FindMember(criteria.UserId);

        if (member != null)
        {
            searchCriteria.UserGroups = member.Groups.ToArray();
            searchCriteria.Visibility = PageDocumentVisibility.Private;
        }

        return await searchService.SearchAllNoCloneAsync(searchCriteria);
    }

    private async Task<Member> FindMember(string userId)
    {
        var userManager = userManagerFactory();
        var user = userManager.Users.FirstOrDefault(x => x.Id == userId);

        if (user != null)
        {
            return await memberService.GetByIdAsync(user.MemberId);
        }

        return null;
    }
}
