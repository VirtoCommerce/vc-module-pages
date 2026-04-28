using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Models;

namespace VirtoCommerce.Pages.Core.ContentProviders;

public interface IPageContentProvider
{
    string ProviderName { get; }
    bool SupportsReindexation { get; }

    Task<PageChangesSearchResult> SearchChangesAsync(PageChangesSearchCriteria criteria);
    Task<IList<PageDocument>> GetByIdsAsync(IList<string> ids);
}
