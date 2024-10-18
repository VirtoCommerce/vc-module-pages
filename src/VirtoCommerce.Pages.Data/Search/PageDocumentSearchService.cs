using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data.Converters;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search;

public class PageDocumentSearchService(ISearchProvider searchProvider,
    PageSearchRequestBuilder requestBuilder, PageDocumentConverter pageDocumentConverter)
    : IPageDocumentSearchService
{
    public virtual async Task<PageDocumentSearchResult> SearchAsync(PageDocumentSearchCriteria criteria, bool clone = true)
    {
        var request = await requestBuilder.BuildRequestAsync(criteria);
        var searchResult = await searchProvider.SearchAsync(ModuleConstants.PageDocumentType, request);

        var result = AbstractTypeFactory<PageDocumentSearchResult>.TryCreateInstance();

        result.Results = searchResult.Documents.Select(pageDocumentConverter.ToPageDocument).ToList();
        result.TotalCount = (int)searchResult.TotalCount;

        return result;
    }

    public virtual async Task<IndexingResult> IndexDocuments(PageDocument[] documents)
    {
        var indexDocuments = documents.Select(pageDocumentConverter.ToIndexDocument).ToList();
        var result = await searchProvider.IndexAsync(ModuleConstants.PageDocumentType, indexDocuments);
        return result;
    }

    public virtual async Task<IndexingResult> RemoveDocuments(string[] documentIds)
    {
        var documents = documentIds.Select(x => new IndexDocument(x)).ToArray();
        var result = await searchProvider.RemoveAsync(ModuleConstants.PageDocumentType, documents);
        return result;
    }
}
