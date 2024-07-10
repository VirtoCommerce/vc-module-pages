using System;
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
            OuterId = (string)document.GetValueSafe("outerid"),
            StoreId = (string)document.GetValueSafe("storeid"),
            CultureName = (string)document.GetValueSafe("culturename"),
            Permalink = (string)document.GetValueSafe("permalink"),
            Title = (string)document.GetValueSafe("title"),
            Description = (string)document.GetValueSafe("description"),
            Content = (string)document.GetValueSafe("content"),
            CreatedBy = (string)document.GetValueSafe("createdby"),
            CreatedDate = (DateTime)document.GetValueSafe("createddate"),
            ModifiedBy = (string)document.GetValueSafe("modifiedby"),
            ModifiedDate = (DateTime?)document.GetValueSafe("modifieddate"),
            Source = (string)document.GetValueSafe("source"),
            MimeType = (string)document.GetValueSafe("mimetype"),
            Status = (PageDocumentStatus)Enum.Parse(typeof(PageDocumentStatus), (string)document.GetValueSafe("status")),
            Visibility = (PageDocumentVisibility)Enum.Parse(typeof(PageDocumentVisibility), (string)document.GetValueSafe("visibility")),
            UserGroups = (string)document.GetValueSafe("usergroups"),
            StartDate = (DateTime?)document.GetValueSafe("startdate"),
            EndDate = (DateTime?)document.GetValueSafe("enddate"),
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
        result.AddRetrievableAndSearchableString(nameof(document.Title), document.Title);
        result.AddRetrievableAndSearchableString(nameof(document.Description), document.Description);
        result.AddRetrievableAndSearchableString(nameof(document.Content), document.Content);
        result.AddFilterableStringAndContentString(nameof(document.CreatedBy), document.CreatedBy);
        result.AddFilterableDateTime(nameof(document.CreatedDate), document.CreatedDate);
        result.AddFilterableStringAndContentString(nameof(document.ModifiedBy), document.ModifiedBy);
        if (document.ModifiedDate.HasValue)
        {
            result.AddFilterableDateTime(nameof(document.ModifiedDate), document.ModifiedDate.Value);
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
            result.AddFilterableDateTime(nameof(document.StartDate), document.StartDate);
        }

        if (document.EndDate.HasValue)
        {
            result.AddFilterableDateTime(nameof(document.EndDate), document.EndDate);
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

    public static void AddRetrievableAndSearchableString(this IndexDocument document, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            document.Add(new IndexDocumentField(name, value, IndexDocumentFieldValueType.String)
            {
                IsRetrievable = true,
                IsSearchable = true,
            });
            document.AddContentString(value);
        }
    }
}
