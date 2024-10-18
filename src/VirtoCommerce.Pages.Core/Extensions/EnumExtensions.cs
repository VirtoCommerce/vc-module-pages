using System;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Models;

namespace VirtoCommerce.Pages.Core.Extensions;

public static class EnumExtensions
{
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

