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
            var matched = await FindFiles(criteria);
            IList<PageDocument> files = matched
                .Where(x => IsAllowed(x, member) && IsServedInLanguage(x, criteria.LanguageCode, store.DefaultLanguage))
                .ToList();
            files = await AddTranslations(criteria, store, member, files, matched);

            return files
                .DistinctBy(x => x.Id)
                .Select(x =>
                {
                    var info = AbstractTypeFactory<SeoInfo>.TryCreateInstance();
                    info.Name = x.Title;
                    info.SemanticUrl = x.Permalink;
                    info.StoreId = x.StoreId;
                    // x-api keeps only records in the requested or the store default language. A version in another
                    // language is served here as the fallback of a missing translation, so it is reported for any language.
                    info.LanguageCode = IsServedInLanguage(x, criteria.LanguageCode, store.DefaultLanguage) ? x.CultureName : null;
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
    /// <param name="files">Documents with the permalink served in the requested language that the visitor may see.</param>
    /// <param name="matched">Documents with the permalink in any language, including the ones hidden from the visitor:
    /// a version hidden by visibility or user group still tells which page the permalink belongs to.</param>
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

        var translations = await FindTranslations(criteria, groupIds, member);

        var requested = OnePerLanguage(translations.Where(x => IsLanguage(x, criteria.LanguageCode)));
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

        var defaultLanguage = OnePerLanguage(translations.Where(x => IsLanguage(x, store.DefaultLanguage)));
        if (defaultLanguage.Count > 0)
        {
            return [.. files, .. defaultLanguage];
        }

        // No version in the default language either: the page is served in the language its permalink belongs to.
        return [.. files, .. OnePerLanguage(matched.Where(x => groupIds.Contains(x.TranslationGroupId) && IsAllowed(x, member)))];
    }

    /// <summary>
    /// Documents with the permalink without the visitor filters (visibility, user groups), which <see cref="IsAllowed"/>
    /// applies afterwards, so a permalink of a hidden translation still leads to the version the visitor may see.
    /// </summary>
    private async Task<IList<PageDocument>> FindFiles(SeoSearchCriteria criteria)
    {
        var permalink = criteria.Permalink?.StartsWith('/') ?? false
            ? criteria.Permalink
            : $"/{criteria.Permalink}";

        var searchCriteria = CreateSearchCriteria(criteria, member: null);
        // Private turns the visibility filter off, null the user group filter.
        searchCriteria.Visibility = PageDocumentVisibility.Private;
        searchCriteria.UserGroups = null;
        searchCriteria.Permalink = permalink;
        searchCriteria.Skip = criteria.Skip;
        searchCriteria.Take = criteria.Take;

        return await searchService.SearchAllNoCloneAsync(searchCriteria);
    }

    /// <summary>
    /// Versions of the pages in the given translation groups in every language, in one index search.
    /// </summary>
    protected virtual async Task<IList<PageDocument>> FindTranslations(SeoSearchCriteria criteria, IList<string> translationGroupIds, Member member)
    {
        var searchCriteria = CreateSearchCriteria(criteria, member);
        searchCriteria.TranslationGroupIds = translationGroupIds;

        return await searchService.SearchAllNoCloneAsync(searchCriteria);
    }

    /// <summary>
    /// Only documents the visitor may see right now: published, in date range, matching visibility and organization.
    /// </summary>
    protected virtual PageDocumentSearchCriteria CreateSearchCriteria(SeoSearchCriteria criteria, Member member)
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
        else
        {
            // Without groups the filter keeps only pages for any group; null would skip it.
            searchCriteria.UserGroups = [];
        }

        return searchCriteria;
    }

    // Uniqueness of (translation group, language) is not guaranteed by the index: the latest modified version wins.
    private static List<PageDocument> OnePerLanguage(IEnumerable<PageDocument> documents)
    {
        return documents
            .GroupBy(x => x.CultureName, StringComparer.OrdinalIgnoreCase)
            .Select(x => x
                .OrderByDescending(document => document.ModifiedDate ?? document.CreatedDate)
                .ThenBy(document => document.Id, StringComparer.Ordinal)
                .First())
            .ToList();
    }

    // Same as the visibility and user group filters of CreateSearchCriteria: a private page only for a signed-in visitor,
    // a page with user groups only for a member of one of them.
    private static bool IsAllowed(PageDocument document, Member member)
    {
        if (member == null && document.Visibility != PageDocumentVisibility.Public)
        {
            return false;
        }

        var groups = member?.Groups ?? [];
        return document.UserGroups is not { Length: > 0 } ||
               document.UserGroups.Any(x => groups.Contains(x, StringComparer.OrdinalIgnoreCase));
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
