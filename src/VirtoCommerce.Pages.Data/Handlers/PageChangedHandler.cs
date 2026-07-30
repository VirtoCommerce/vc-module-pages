using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data.Caching;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Pages.Data.Handlers
{
    public class PageChangedHandler(IPageDocumentSearchService searchService) : IEventHandler<PagesDomainEvent>
    {
        public async Task Handle(PagesDomainEvent message)
        {
            // A page changed, so the cached index change-list (PageIndexDocumentChangesProvider) is now
            // stale. Invalidate it here so a subsequent full rebuild re-queries the providers instead of
            // reusing a list that was cached before this change (e.g. an empty list cached by a scheduled
            // sync tick that ran before the page was committed, which would otherwise leave the index empty).
            PagesCacheRegion.ExpireRegion();

            if (message.Page != null)
            {
                switch (message.Operation)
                {
                    case PageOperation.Delete:
                        await searchService.RemoveDocuments([message.Page.Id]);
                        break;
                    default:
                        await searchService.IndexDocuments([message.Page]);
                        break;
                }
            }

        }
    }
}
