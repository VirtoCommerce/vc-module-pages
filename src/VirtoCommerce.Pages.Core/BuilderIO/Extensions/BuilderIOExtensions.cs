using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Pages.Core.BuilderIO.Models;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Models;

namespace VirtoCommerce.Pages.Core.BuilderIO.Extensions;

public static class BuilderIOExtensions
{
    public static PageOperation ToPageOperation(this string value)
    {
        return value switch
        {
            "publish" => PageOperation.Publish,
            "archive" => PageOperation.Archive,
            "delete" => PageOperation.Delete,
            "unpublish" => PageOperation.Unpublish,
            "scheduledStart" => PageOperation.ScheduledStart,
            "scheduledEnd" => PageOperation.ScheduledEnd,
            _ => PageOperation.Unknown,
        };
    }

    public static PageDocument FromBuilderIOPage(this BuilderIOPage value, string storeId, string cultureName, PageOperation operation = PageOperation.Unknown)
    {
        return new PageDocument
        {
            Content = value.Data?.GetValueOrDefault("blocksString")?.ToString(),
            CreatedBy = value.CreatedBy,
            CreatedDate = value.CreatedDate,
            Id = value.Id,
            OuterId = value.Id,
            Permalink = value.Query
                ?.FirstOrDefault(x => x.Property == "urlPath" && x.Operator == "is")?.Value,
            Title = value.Data?.GetValueOrDefault("title")?.ToString(),
            Description = value.Data?.GetValueOrDefault("description")?.ToString(),
            Status = operation.GetPageDocumentStatus(),
            MimeType = "application/json",
            ModifiedBy = value.LastUpdatedBy,
            ModifiedDate = value.LastUpdated,
            Source = "builder.io",
            Visibility = PageDocumentVisibility.Public,
            StoreId = storeId,
            CultureName = cultureName,
            EndDate = value.StartDate,
            StartDate = value.EndDate == DateTime.MinValue ? DateTime.MaxValue : value.EndDate,
            // UserGroups = 
        };
    }

    public static PageDocumentStatus GetPageDocumentStatus(this PageOperation operation)
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

