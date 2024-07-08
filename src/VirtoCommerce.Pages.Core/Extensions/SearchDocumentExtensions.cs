using System;
using System.Globalization;
using System.Linq;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.Pages.Core.Extensions;

public static class SearchDocumentExtensions
{
    public static PageDocumentSearchResult ToPageDocumentSearchResult(this SearchResponse searchResponse)
    {
        var result = new PageDocumentSearchResult
        {
            Results = searchResponse.Documents.Select(x => x.ToPageDocument()).ToList(),
            TotalCount = (int)searchResponse.TotalCount
        };
        return result;
    }

    public static PageDocument ToPageDocument(this SearchDocument document)
    {
        var result = new PageDocument
        {
            Id = document.Id,
            OuterId = (string)document.GetValueSafe("outerId"),
            StoreId = (string)document.GetValueSafe("storeId"),
            CultureName = (string)document.GetValueSafe("cultureName"),
            Permalink = (string)document.GetValueSafe("permalink"),
            Title = (string)document.GetValueSafe("title"),
            Description = (string)document.GetValueSafe("description"),
            Content = (string)document.GetValueSafe("content"),
            CreatedBy = (string)document.GetValueSafe("createdBy"),
            CreatedDate = document.GetValueSafe("createdDate").ToDateTime()!.Value,
            ModifiedBy = (string)document.GetValueSafe("modifiedBy"),
            ModifiedDate = document.GetValueSafe("modifiedDate").ToDateTime(),
            Source = (string)document.GetValueSafe("source"),
            MimeType = (string)document.GetValueSafe("mimeType"),
            Status = (PageDocumentStatus)Enum.Parse(typeof(PageDocumentStatus), (string)document.GetValueSafe("status")),
            Visibility = (PageDocumentVisibility)Enum.Parse(typeof(PageDocumentVisibility), (string)document.GetValueSafe("visibility")),
            UserGroups = (string)document.GetValueSafe("userGroups"),
            StartDate = document.GetValueSafe("startDate").ToDateTime(),
            EndDate = document.GetValueSafe("endDate").ToDateTime(),
        };

        if (result.UserGroups == "__any")
        {
            result.UserGroups = null;
        }

        if (result.CultureName == "__any")
        {
            result.CultureName = null;
        }

        return result;
    }

    public static IndexDocument ToIndexDocument(this PageDocument document)
    {
        var result = new IndexDocument(document.Id);
        result.AddFilterableStringAndContentString(nameof(document.OuterId), document.OuterId);
        result.AddFilterableStringAndContentString(nameof(document.StoreId), document.StoreId);
        result.AddFilterableStringAndContentString(nameof(document.CultureName), document.CultureName ?? "__any");
        result.AddFilterableStringAndContentString(nameof(document.Permalink), document.Permalink.Permalink());
        result.AddContentString(document.Title);
        result.AddContentString(document.Description);
        result.AddContentString(document.Content);
        result.AddFilterableStringAndContentString(nameof(document.CreatedBy), document.CreatedBy);
        result.AddFilterableStringAndContentString(nameof(document.CreatedDate), document.CreatedDate.ToDateString());
        result.AddFilterableStringAndContentString(nameof(document.ModifiedBy), document.ModifiedBy);
        if (document.ModifiedDate.HasValue)
        {
            result.AddFilterableStringAndContentString(nameof(document.ModifiedDate), document.ModifiedDate.Value.ToDateString());
        }

        result.AddFilterableStringAndContentString(nameof(document.Source), document.Source);
        result.AddFilterableStringAndContentString(nameof(document.MimeType), document.MimeType);
        result.AddFilterableStringAndContentString(nameof(document.Status), document.Status.ToString());
        result.AddFilterableStringAndContentString(nameof(document.Visibility), document.Visibility.ToString());

        var userGroups = !document.UserGroups.IsNullOrEmpty()
            ? document.UserGroups
            : "__any";
        result.AddFilterableStringAndContentString(nameof(document.UserGroups), userGroups);

        if (document.StartDate.HasValue)
        {
            result.AddFilterableStringAndContentString(nameof(document.StartDate), document.StartDate.Value.ToDateString());
        }

        if (document.EndDate.HasValue)
        {
            result.AddFilterableStringAndContentString(nameof(document.EndDate), document.EndDate.Value.ToDateString());
        }
        return result;
    }

    private static string Permalink(this string value)
    {
        if (value != null && !value.StartsWith('/'))
        {
            return $"/{value}";
        }

        return value;
    }

    private static string ToDateString(this DateTime value)
    {
        return value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    private static DateTime? ToDateTime(this object value)
    {
        if (value == null)
        {
            return null;
        }
        if (value is DateTime dateTime)
        {
            return dateTime;
        }
        if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, out dateTime))
        {
            return dateTime;
        }
        return null;
    }
}
