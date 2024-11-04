using System;
using System.Collections.Generic;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Pages.Core;

public static class ModuleConstants
{
    public static string PageDocumentType => "Pages";

    public static class Security
    {
        public static string ModuleGroup => "Pages";

        public static class Permissions
        {
            public const string Create = "Pages:create";
            public const string Read = "Pages:read";
            public const string Update = "Pages:update";
            public const string Delete = "Pages:delete";

            public static string[] AllPermissions { get; } =
            {
                Create,
                Read,
                Update,
                Delete,
            };
        }
    }

    public static class Settings
    {
        public static class Search
        {
            public static SettingDescriptor IndexationDatePages { get; } = new()
            {
                Name = $"VirtoCommerce.Search.IndexingJobs.IndexationDate.{nameof(PageDocument)}",
                GroupName = "Pages|Search",
                ValueType = SettingValueType.DateTime,
                DefaultValue = default(DateTime),
            };
        }

        public static class General
        {
            public static SettingDescriptor Enable { get; } = new()
            {
                Name = "VirtoPages.Enable",
                GroupName = "VirtoPages",
                ValueType = SettingValueType.Boolean,
                IsPublic = true,
                DefaultValue = false,
            };
        }

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return Search.IndexationDatePages;
                yield return General.Enable;
            }
        }

        public static IEnumerable<SettingDescriptor> StoreLevelSettings
        {
            get
            {
                yield return General.Enable;
            }
        }
    }


}
