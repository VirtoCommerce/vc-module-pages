using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Caching;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search;

public class PageIndexDocumentChangesProvider(
    IEnumerable<IPageContentProvider> contentProviders,
    ISettingsManager settingsManager,
    IPlatformMemoryCache platformMemoryCache)
    : IIndexDocumentChangesProvider
{
    private const int MaxChangesPerProvider = 10_000;

    public async Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate)
    {
        var providers = GetProvidersForOperation(startDate);
        if (providers.Count == 0)
        {
            return 0;
        }

        // Use the same cached data as GetChangesAsync to keep total and changes consistent
        var allChanges = await GetAllChangesCachedAsync(providers, startDate, endDate);
        return allChanges.Count;
    }

    public async Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take)
    {
        var providers = GetProvidersForOperation(startDate);
        if (providers.Count == 0)
        {
            return [];
        }

        var allChanges = await GetAllChangesCachedAsync(providers, startDate, endDate);

        return allChanges
            .Skip(Convert.ToInt32(skip))
            .Take(Convert.ToInt32(take))
            .ToList();
    }

    private Task<IList<IndexDocumentChange>> GetAllChangesCachedAsync(
        IReadOnlyList<IPageContentProvider> providers,
        DateTime? startDate,
        DateTime? endDate)
    {
        var cacheKey = CacheKey.With(GetType(), nameof(GetAllChangesCachedAsync),
            startDate?.ToString("O") ?? "null",
            endDate?.ToString("O") ?? "null");

        return platformMemoryCache.GetOrCreateExclusiveAsync(cacheKey, async cacheEntry =>
        {
            cacheEntry.AddExpirationToken(PagesCacheRegion.CreateChangeToken());
            cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            var criteria = AbstractTypeFactory<PageChangesSearchCriteria>.TryCreateInstance();
            criteria.StartDate = startDate;
            criteria.EndDate = endDate;
            criteria.Skip = 0;
            criteria.Take = MaxChangesPerProvider;

            var allChanges = new List<IndexDocumentChange>();

            foreach (var provider in providers)
            {
                var result = await provider.SearchChangesAsync(criteria);
                allChanges.AddRange(result.Results);
            }

            allChanges.Sort((a, b) => b.ChangeDate.CompareTo(a.ChangeDate));

            return (IList<IndexDocumentChange>)allChanges;
        });
    }

    private List<IPageContentProvider> GetProvidersForOperation(DateTime? startDate)
    {
        var isFullReindex = startDate == null;
        var isIncrementalSync = !isFullReindex;

        // For incremental sync (scheduled), check if sync is enabled
        if (isIncrementalSync)
        {
            var syncEnabled = settingsManager.GetValue<bool>(ModuleConstants.Settings.General.ScheduledSyncEnabled);
            if (!syncEnabled)
            {
                return [];
            }
        }

        var providers = contentProviders.ToList();

        // For full reindex, validate all providers support it
        if (isFullReindex)
        {
            var unsupported = providers.Where(p => !p.SupportsReindexation).ToList();
            if (unsupported.Count > 0)
            {
                var names = string.Join(", ", unsupported.Select(p => p.ProviderName));
                throw new InvalidOperationException(
                    $"The following content providers do not support reindexation: {names}. " +
                    "Full index rebuild cannot be performed.");
            }
        }

        return providers;
    }
}
