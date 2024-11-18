using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.Pages.Core.Extensions;

public static class SearchCriteriaExtensions
{
    public static async Task EnrichAuth(this PageDocumentSearchCriteria criteria, ApplicationUser user, IMemberService memberService)
    {
        if (user != null)
        {
            var member = await memberService.GetByIdAsync(user.MemberId);
            if (member != null)
            {
                criteria.UserGroups = member.Groups.ToArray();
            }

            if (user.IsAdministrator)
            {
                criteria.UserGroups = null;
            }

            criteria.Visibility = PageDocumentVisibility.Private;
        }
        else
        {
            criteria.UserGroups = [];
        }

    }
}
