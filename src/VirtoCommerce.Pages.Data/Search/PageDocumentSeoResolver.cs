using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Pages.Data.Search;

public class PageDocumentSeoResolver(IPageDocumentSearchService searchService) : ISeoResolver
{
    public async Task<IList<SeoInfo>> FindSeoAsync(SeoSearchCriteria criteria)
    {
        return (await FindFiles(criteria))
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
                info.IsActive = x.Status == PageDocumentStatus.Published && x.Visibility == PageDocumentVisibility.Public;
                info.ObjectType = ModuleConstants.PageDocumentType;
                info.CreatedBy = x.CreatedBy;
                info.CreatedDate = x.CreatedDate;
                return info;
            })
            .OrderBy(x => x.LanguageCode)
            .ToList();
    }

    private Task<IList<PageDocument>> FindFiles(SeoSearchCriteria criteria)
    {
        var permalink = criteria.Permalink?.StartsWith("/") ?? false
            ? criteria.Permalink
            : $"/{criteria.Permalink}";

        var searchCriteria = AbstractTypeFactory<PageDocumentSearchCriteria>.TryCreateInstance();
        searchCriteria.StoreId = criteria.StoreId;
        searchCriteria.LanguageCode = criteria.LanguageCode;
        searchCriteria.Permalink = permalink;
        searchCriteria.Skip = criteria.Skip;
        searchCriteria.Take = criteria.Take;

        return searchService.SearchAllNoCloneAsync(searchCriteria);
    }
}
