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
using VirtoCommerce.StoreModule.Core.Model;
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
            var member = await FindMember(criteria.UserId);
            // Every slug (products, categories) comes here first, so the permalink is searched once in every language
            // and the language filter of the index (language, store default, any) is applied here.
            var matched = await FindFiles(criteria, member);
            IList<PageDocument> files = matched.Where(x => IsServedInLanguage(x, criteria.LanguageCode, store.DefaultLanguage)).ToList();
            files = await AddTranslations(criteria, store, member, files, matched);

            return files
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
                    info.OrganizationId = x.OrganizationId;
                    // the FindFiles method always returns only active documents
                    info.IsActive = true;
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

    /// <summary>
    /// A visitor who switches language keeps the permalink of the previous language. When the permalink belongs to a page
    /// with translations, the version in the requested language is added; its own permalink lets the storefront move to it.
    /// </summary>
    /// <param name="files">Documents with the permalink served in the requested language.</param>
    /// <param name="matched">Documents with the permalink in any language.</param>
    protected virtual async Task<IList<PageDocument>> AddTranslations(SeoSearchCriteria criteria, Store store, Member member,
        IList<PageDocument> files, IList<PageDocument> matched)
    {
        if (string.IsNullOrEmpty(criteria.LanguageCode) || files.Any(x => IsLanguage(x, criteria.LanguageCode)))
        {
            return files;
        }

        var groupIds = matched
            .Select(x => x.TranslationGroupId)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .ToList();

        if (groupIds.Count == 0)
        {
            return files;
        }

        var translations = new List<PageDocument>();
        foreach (var groupId in groupIds)
        {
            translations.AddRange(await FindTranslations(criteria, groupId, member));
        }

        var requested = translations.Where(x => IsLanguage(x, criteria.LanguageCode)).ToList();
        if (requested.Count > 0)
        {
            return [.. files, .. requested];
        }

        var behavior = store.Settings.GetValue<string>(ModuleConstants.Settings.General.MissingTranslationBehavior);
        if (behavior == ModuleConstants.MissingTranslationBehaviors.NotFound)
        {
            // The same page must not be served in another language.
            return files.Where(x => !groupIds.Contains(x.TranslationGroupId)).ToList();
        }

        var defaultLanguage = translations.Where(x => IsLanguage(x, store.DefaultLanguage)).ToList();
        if (defaultLanguage.Count > 0)
        {
            return [.. files, .. defaultLanguage];
        }

        // No version in the default language either: the page is served in the language its permalink belongs to.
        return [.. files, .. matched.Where(x => groupIds.Contains(x.TranslationGroupId))];
    }

    private async Task<IList<PageDocument>> FindFiles(SeoSearchCriteria criteria, Member member)
    {
        var permalink = criteria.Permalink?.StartsWith('/') ?? false
            ? criteria.Permalink
            : $"/{criteria.Permalink}";

        var searchCriteria = CreateSearchCriteria(criteria, member);
        searchCriteria.Permalink = permalink;
        searchCriteria.Skip = criteria.Skip;
        searchCriteria.Take = criteria.Take;

        return await searchService.SearchAllNoCloneAsync(searchCriteria);
    }

    private async Task<IList<PageDocument>> FindTranslations(SeoSearchCriteria criteria, string translationGroupId, Member member)
    {
        var searchCriteria = CreateSearchCriteria(criteria, member);
        searchCriteria.TranslationGroupId = translationGroupId;

        return await searchService.SearchAllNoCloneAsync(searchCriteria);
    }

    // Only documents the visitor may see right now: published, in date range, matching visibility and organization.
    private static PageDocumentSearchCriteria CreateSearchCriteria(SeoSearchCriteria criteria, Member member)
    {
        var searchCriteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        searchCriteria.StoreId = criteria.StoreId;
        searchCriteria.CertainDate = DateTime.UtcNow;
        searchCriteria.Status = PageDocumentStatus.Published;
        searchCriteria.OrganizationId = criteria.OrganizationId;

        if (member != null)
        {
            searchCriteria.UserGroups = member.Groups.ToArray();
            searchCriteria.Visibility = PageDocumentVisibility.Private;
        }

        return searchCriteria;
    }

    // Same as the language filter of the index: a document for any language, the requested one or the store default.
    private static bool IsServedInLanguage(PageDocument document, string languageCode, string defaultLanguage)
    {
        return string.IsNullOrEmpty(languageCode) ||
               string.IsNullOrEmpty(document.CultureName) ||
               IsLanguage(document, languageCode) ||
               IsLanguage(document, defaultLanguage);
    }

    private static bool IsLanguage(PageDocument document, string languageCode)
    {
        return string.Equals(document.CultureName, languageCode, StringComparison.OrdinalIgnoreCase);
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
