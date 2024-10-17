using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Pages.Data.Handlers
{
    public class PageChangedHandler(IPageDocumentSearchService searchService) : IEventHandler<PagesDomainEvent>
    {
        public async Task Handle(PagesDomainEvent message)
        {
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
