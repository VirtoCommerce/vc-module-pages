using System.Collections.Generic;

namespace VirtoCommerce.Pages.Core.Models;

public class GetPageDocumentsResponse
{
    public int TotalCount { get; set; }
    public IEnumerable<PageDocument> Pages { get; set; }
}
