using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Pages.Core.Models;

public class PageChangesSearchCriteria : SearchCriteriaBase
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
