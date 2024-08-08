using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.Pages.Core.BuilderIO.Extensions;
using VirtoCommerce.Pages.Core.BuilderIO.Models;
using VirtoCommerce.Pages.Core.Events;
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
                Page = (model.NewValue ?? model.PreviousValue).FromBuilderIOPage(storeId, cultureName, operation),
            };
            await eventPublisher.Publish(pageChangedEvent);
        }

        return Ok();
    }
}

