using System;
using System.Linq;
using VirtoCommerce.Pages.Core.Converters;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Extensions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.Pages.Data.Converters;

public class PageDocumentConverter : IPageDocumentConverter
{
    public const string Any = "__any";

    public virtual PageDocument ToPageDocument(SearchDocument searchDocument)
    {
        var result = AbstractTypeFactory<PageDocument>.TryCreateInstance();
        result.Id = searchDocument.Id;
        result.OuterId = (string)searchDocument.GetValueSafe("outerid");
        result.StoreId = (string)searchDocument.GetValueSafe("storeid");
        result.CultureName = (string)searchDocument.GetValueSafe("culturename");
        result.Permalink = (string)searchDocument.GetValueSafe("permalink");
        result.Title = (string)searchDocument.GetValueSafe("title");
        result.Description = (string)searchDocument.GetValueSafe("description");
        result.Content = (string)searchDocument.GetValueSafe("content");
        result.CreatedBy = (string)searchDocument.GetValueSafe("createdby");
        result.CreatedDate = searchDocument.GetDateSafe("createddate") ?? DateTime.MinValue;
        result.ModifiedBy = (string)searchDocument.GetValueSafe("modifiedby");
        result.ModifiedDate = searchDocument.GetDateSafeOrNull("modifieddate");
        result.Source = (string)searchDocument.GetValueSafe("source");
        result.MimeType = (string)searchDocument.GetValueSafe("mimetype");
        result.Status = (PageDocumentStatus)Enum.Parse(typeof(PageDocumentStatus), (string)searchDocument.GetValueSafe("status"));
        result.Visibility = (PageDocumentVisibility)Enum.Parse(typeof(PageDocumentVisibility), (string)searchDocument.GetValueSafe("visibility"));
        var userGroupsValue = searchDocument.GetValueSafe("usergroups");
        result.UserGroups = userGroupsValue is string userGroups
            ? [userGroups]
            : ((object[])userGroupsValue).Cast<string>().ToArray();
        result.StartDate = searchDocument.GetDateSafeOrNull("startdate");
        result.EndDate = searchDocument.GetDateSafeOrNull("enddate");

        if (result.UserGroups != null && result.UserGroups.Length == 1 && result.UserGroups[0] == Any)
        {
            result.UserGroups = [];
        }

        if (result.CultureName == Any)
        {
            result.CultureName = null;
        }

        return result;
    }

    public virtual IndexDocument ToIndexDocument(PageDocument pageDocument)
    {
        var result = new IndexDocument(pageDocument.Id);
        result.AddFilterableStringAndContentString(nameof(pageDocument.OuterId), pageDocument.OuterId);
        result.AddFilterableStringAndContentString(nameof(pageDocument.StoreId), pageDocument.StoreId);
        result.AddFilterableStringAndContentString(nameof(pageDocument.CultureName), pageDocument.CultureName ?? Any);
        result.AddFilterableStringAndContentString(nameof(pageDocument.Permalink), pageDocument.Permalink.Permalink());
        result.AddRetrievableAndSearchableString(nameof(pageDocument.Title), pageDocument.Title);
        result.AddRetrievableAndSearchableString(nameof(pageDocument.Description), pageDocument.Description);
        result.AddRetrievableAndSearchableString(nameof(pageDocument.Content), pageDocument.Content);
        result.AddFilterableStringAndContentString(nameof(pageDocument.CreatedBy), pageDocument.CreatedBy);
        result.AddFilterableDateTime(nameof(pageDocument.CreatedDate), pageDocument.CreatedDate);
        result.AddFilterableStringAndContentString(nameof(pageDocument.ModifiedBy), pageDocument.ModifiedBy);
        if (pageDocument.ModifiedDate.HasValue)
        {
            result.AddFilterableDateTime(nameof(pageDocument.ModifiedDate), pageDocument.ModifiedDate.Value);
        }

        result.AddFilterableStringAndContentString(nameof(pageDocument.Source), pageDocument.Source);
        result.AddFilterableStringAndContentString(nameof(pageDocument.MimeType), pageDocument.MimeType);
        result.AddFilterableStringAndContentString(nameof(pageDocument.Status), pageDocument.Status.ToString());
        result.AddFilterableStringAndContentString(nameof(pageDocument.Visibility), pageDocument.Visibility.ToString());

        var userGroups = pageDocument.UserGroups is { Length: > 0 }
            ? pageDocument.UserGroups
            : [Any];
        result.AddFilterableCollectionAndContentString(nameof(pageDocument.UserGroups), userGroups);

        result.AddFilterableDateTime(nameof(pageDocument.StartDate), pageDocument.StartDate ?? DateTime.MinValue);
        result.AddFilterableDateTime(nameof(pageDocument.EndDate), pageDocument.EndDate ?? DateTime.MaxValue);

        return result;
    }
}
