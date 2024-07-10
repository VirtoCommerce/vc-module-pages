using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Pages.Core.Models;

public class PageDocumentSearchCriteria : SearchCriteriaBase
{
    public string StoreId { get; set; }
    public string Permalink { get; set; }
    public PageDocumentVisibility? Visibility { get; set; }
    public PageDocumentStatus? Status { get; set; }
    public string[] UserGroups { get; set; }
    //public string FolderUrl { get; set; }
}
