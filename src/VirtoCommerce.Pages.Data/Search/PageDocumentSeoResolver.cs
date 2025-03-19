using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search;

public class PageDocumentSeoResolver(IPageDocumentSearchService searchService,
    Func<UserManager<ApplicationUser>> userManagerFactory,
    IStoreService storeService,
    IMemberService memberService
    ) : ISeoResolver
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
            return (await FindFiles(criteria))
                .DistinctBy(x => x.Id)
                .Select(x =>
                {
                    var info = AbstractTypeFactory<SeoInfo>.TryCreateInstance();
                    info.Name = x.Title;
                    info.SemanticUrl = x.Permalink;
                    info.StoreId = x.StoreId;
                    info.LanguageCode = x.CultureName;
                    info.ObjectId = x.Id;
                    info.Id = x.Id;
                    info.IsActive = x.Status == PageDocumentStatus.Published &&
                                    x.Visibility == PageDocumentVisibility.Public;
                    info.ObjectType = ModuleConstants.PageDocumentType;
                    info.CreatedBy = x.CreatedBy;
                    info.CreatedDate = x.CreatedDate;
                    return info;
                })
                .OrderBy(x => x.LanguageCode)
                .ToList();
        }

        return [];
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

        var member = await FindMember(criteria.UserId);
        if (member != null)
        {
            searchCriteria.UserGroups = member.Groups.ToArray();
            searchCriteria.Visibility = PageDocumentVisibility.Private;
        }

        var result = await searchService.SearchAllNoCloneAsync(searchCriteria);

        return result;
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
