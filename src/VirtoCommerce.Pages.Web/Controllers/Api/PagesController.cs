using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core.Extensions;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.Pages.Web.Controllers.Api;

[Authorize]
[Route("api/pages")]
public class PagesController(UserManager<ApplicationUser> userManager,
    IMemberService memberService,
    IPageDocumentSearchService searchService) : Controller
{
    [HttpPost]
    [Route("search")]
    public async Task<ActionResult<PageDocumentSearchResult>> Search([FromBody] PageDocumentSearchCriteria criteria)
    {
        var user = await userManager.FindByNameAsync(User.Identity.Name);
        await criteria.EnrichAuth(user, memberService);
        var result = await searchService.SearchAsync(criteria);
        return Ok(result);
    }
}
