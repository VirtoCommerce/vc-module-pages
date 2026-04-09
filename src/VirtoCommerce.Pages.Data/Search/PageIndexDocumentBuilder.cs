using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Converters;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search;

public class PageIndexDocumentBuilder(
    IPageContentProviderRegistrar providerRegistrar,
    IPageDocumentConverter documentConverter,
    ILogger<PageIndexDocumentBuilder> logger)
    : IIndexDocumentBuilder
{
    public async Task<IList<IndexDocument>> GetDocumentsAsync(IList<string> documentIds)
    {
        var result = new ConcurrentBag<IndexDocument>();
        var providers = providerRegistrar.GetProviders();

        await Parallel.ForEachAsync(providers, async (provider, _) =>
        {
            try
            {
                var pages = await provider.GetByIdsAsync(documentIds);

                foreach (var page in pages)
                {
                    if (string.IsNullOrEmpty(page.StoreId))
                    {
                        logger.LogWarning("Skipping page '{PageId}' from provider '{ProviderName}': StoreId is required",
                            page.Id, provider.ProviderName);
                        continue;
                    }

                    var indexDocument = documentConverter.ToIndexDocument(page);
                    result.Add(indexDocument);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting pages from content provider '{ProviderName}'", provider.ProviderName);
            }
        });

        return result.ToList();
    }
}
