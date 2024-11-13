using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;

namespace VirtoCommerce.Pages.Web.Controllers.Api;

[Authorize]
[Route("api/pages")]
public class PagesController(IPageDocumentSearchService searchService) : Controller
{
    [HttpPost]
    [Route("search")]
    public async Task<ActionResult<PageDocumentSearchResult>> Search([FromBody] PageDocumentSearchCriteria criteria)
    {
        var result = await searchService.SearchAsync(criteria);
        return Ok(result);
    }


}

