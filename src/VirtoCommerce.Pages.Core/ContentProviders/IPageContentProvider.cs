using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.Pages.Core.ContentProviders;

public interface IPageContentProvider
{
    string ProviderName { get; }
    bool SupportsReindexation { get; }

    Task<long> GetTotalChangesCountAsync(DateTime? startDate, DateTime? endDate);
    Task<IList<IndexDocumentChange>> GetChangesAsync(DateTime? startDate, DateTime? endDate, long skip, long take);
    Task<IList<PageDocument>> GetByIdsAsync(IList<string> ids);
}
