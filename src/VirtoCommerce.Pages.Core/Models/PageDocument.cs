using System;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Pages.Core.Models;

public class PageDocument : IEntity, IAuditable
{
    public string Id { get; set; }
    public string OuterId { get; set; }
    public string StoreId { get; set; }
    public string CultureName { get; set; }
    public string Permalink { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public PageDocumentStatus Status { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string CreatedBy { get; set; }
    public string ModifiedBy { get; set; }

    public string Source { get; set; }
    public string MimeType { get; set; }
    public string Content { get; set; }
    public PageDocumentVisibility Visibility { get; set; }

    public string UserGroups { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // metadata??
}
