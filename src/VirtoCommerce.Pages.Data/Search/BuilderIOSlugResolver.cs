using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;

namespace VirtoCommerce.Pages.Data.Search;

public class BuilderIOSlugResolver(IPageDocumentSearchService searchService) : ISeoBySlugResolver
{
    public async Task<SeoInfo[]> FindSeoBySlugAsync(string slug)
    {
        if (string.IsNullOrEmpty(slug))
        {
            return [];
        }
        if (!slug.StartsWith("/"))
        {
            slug = "/" + slug;
        }

        var criteria = new PageDocumentSearchCriteria
        {
            Permalink = slug,
            Skip = 0,
            Take = 20
        };

        var result = await searchService.SearchAsync(criteria);
        return result.Results.Select(x => new SeoInfo
        {
            Id = x.Id,
            SemanticUrl = x.Permalink,
            IsActive = true,
            LanguageCode = x.CultureName,
            StoreId = x.StoreId,
            ObjectType = "ContentFile",
            ObjectId = x.Id
        }).ToArray();
    }
}
