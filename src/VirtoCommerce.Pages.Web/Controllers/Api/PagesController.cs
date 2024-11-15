using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.CustomerModule.Core.Services;
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
        var currentUser = await userManager.FindByNameAsync(User.Identity.Name);
        if (currentUser != null)
        {
            var member = await memberService.GetByIdAsync(currentUser.MemberId);
            if (member != null)
            {
                criteria.UserGroups = member.Groups.ToArray();
            }

            if (currentUser.IsAdministrator)
            {
                criteria.UserGroups = null;
            }

            criteria.Visibility = PageDocumentVisibility.Private;
        }
        else
        {
            criteria.UserGroups = [];
        }

        var result = await searchService.SearchAsync(criteria);
        return Ok(result);
    }
}
