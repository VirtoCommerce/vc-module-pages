using System.Threading.Tasks;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.GenericCrud;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.Pages.Core.Search;

public interface IPageDocumentSearchService : ISearchService<PageDocumentSearchCriteria, PageDocumentSearchResult, PageDocument>
{
    Task<IndexingResult> IndexDocuments(PageDocument[] documents);
    Task<IndexingResult> RemoveDocuments(string[] documentIds);
}
