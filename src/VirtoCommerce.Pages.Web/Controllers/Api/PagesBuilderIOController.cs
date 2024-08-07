using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Pages.Core.BuilderIO.Extensions;
using VirtoCommerce.Pages.Core.BuilderIO.Models;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Pages.Web.Controllers.Api;

[Route("api/pages/builder-io")]
public class BuilderIOController(IEventPublisher eventPublisher) : Controller
{
    [HttpPost]
    public async Task<ActionResult> Post([FromBody] BuilderIOPageChanges model, [FromHeader] string storeId, [FromHeader] string cultureName)
    {
        if (model?.ModelName == "page")
        {
            var operation = model.Operation.ToPageOperation();
            var pageChangedEvent = new PagesDomainEvent
            {
                Operation = operation,
                Page = new PageDocument
                {
                    Content = model.NewValue.Data.BlocksString,
                    CreatedBy = model.NewValue.CreatedBy,
                    CreatedDate = model.NewValue.CreatedDate,
                    Id = model.NewValue.Id,
                    OuterId = model.NewValue.Id,
                    Permalink = model.NewValue.Query?.FirstOrDefault(x => x.Property == "urlPath")?.Value,
                    Title = model.NewValue.Data.Title,
                    CultureName = cultureName,
                    StoreId = storeId,
                    Status = GetPageDocumentStatus(operation),
                    MimeType = "application/json",
                    ModifiedBy = model.NewValue.LastUpdatedBy,
                    ModifiedDate = model.NewValue.LastUpdated,
                    Source = "builder.io",
                    Visibility = PageDocumentVisibility.Public,
                    // Description =
                    // EndDate =
                    // StartDate =
                    // UserGroups = 
                }
            };
            await eventPublisher.Publish(pageChangedEvent);
        }

        return Ok();
    }

    private PageDocumentStatus GetPageDocumentStatus(PageOperation operation)
    {
        return operation switch
        {
            PageOperation.Publish => PageDocumentStatus.Published,
            PageOperation.Archive => PageDocumentStatus.Archived,
            PageOperation.Delete => PageDocumentStatus.Deleted,
            PageOperation.Unpublish => PageDocumentStatus.Draft,
            PageOperation.ScheduledStart => PageDocumentStatus.Published,
            PageOperation.ScheduledEnd => PageDocumentStatus.Archived,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }
}

