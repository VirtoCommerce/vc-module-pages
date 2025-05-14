using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;

namespace VirtoCommerce.Pages.Data.Extensions;

internal static class ConverterExtensions
{
    public static string Permalink(this string value)
    {
        if (value != null && !value.StartsWith('/'))
        {
            return $"/{value}";
        }

        return value;
    }

    public static DateTime? GetDateSafeOrNull(this IDictionary<string, object> dictionary, string key)
    {
        var result = dictionary.GetDateSafe(key);

        if (result == DateTime.MinValue || result == DateTime.MaxValue)
        {
            return null;
        }

        return result;
    }

    public static DateTime? GetDateSafe(this IDictionary<string, object> dictionary, string key)
    {
        var value = dictionary.GetValueSafe(key);
        if (value is DateTime date)
        {
            return date;
        }
        if (value is string stringValue && DateTime.TryParse(stringValue, out date))
        {
            return date;
        }
        return null;
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
