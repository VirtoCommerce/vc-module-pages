using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Pages.Core.Models;

public class PageDocumentSearchCriteria : SearchCriteriaBase
{
    public string StoreId { get; set; }
    public string Permalink { get; set; }
    public string OrganizationId { get; set; }

    /// <summary>
    /// Filter by organization even when <see cref="OrganizationId"/> is empty: the result then has only pages
    /// for any organization, which is what a storefront visitor without an organization may get. When false,
    /// an empty <see cref="OrganizationId"/> does not filter by organization at all.
    /// </summary>
    public bool FilterByOrganization { get; set; }
    public PageDocumentVisibility Visibility { get; set; } = PageDocumentVisibility.Public;
    public PageDocumentStatus? Status { get; set; }
    public DateTime? CertainDate { get; set; }
    public string[] UserGroups { get; set; }
}
