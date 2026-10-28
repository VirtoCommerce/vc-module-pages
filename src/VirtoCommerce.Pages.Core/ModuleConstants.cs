using System;
using System.Collections.Generic;
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
                Name = $"VirtoCommerce.Search.IndexingJobs.IndexationDate.{PageDocumentType}",
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
                GroupName = "Pages",
                ValueType = SettingValueType.Boolean,
                IsPublic = true,
                DefaultValue = false,
            };

            /// <summary>
            /// What a visitor gets when a page has translations but none in the requested language:
            /// the default language version, or not found.
            /// </summary>
            public static SettingDescriptor MissingTranslationBehavior { get; } = new()
            {
                Name = "VirtoPages.MissingTranslationBehavior",
                GroupName = "Pages",
                ValueType = SettingValueType.ShortText,
                IsPublic = true,
                DefaultValue = MissingTranslationBehaviors.DefaultLanguage,
                AllowedValues = [MissingTranslationBehaviors.DefaultLanguage, MissingTranslationBehaviors.NotFound],
            };

            public static SettingDescriptor ScheduledSyncEnabled { get; } = new()
            {
                Name = "VirtoPages.ScheduledSync.Enable",
                GroupName = "Pages|Scheduled Sync",
                ValueType = SettingValueType.Boolean,
                DefaultValue = false,
            };
        }

        public static IEnumerable<SettingDescriptor> AllSettings
        {
            get
            {
                yield return Search.IndexationDatePages;
                yield return General.Enable;
                yield return General.MissingTranslationBehavior;
                yield return General.ScheduledSyncEnabled;
            }
        }

        public static IEnumerable<SettingDescriptor> StoreLevelSettings
        {
            get
            {
                yield return General.Enable;
                yield return General.MissingTranslationBehavior;
            }
        }
    }

    public static class MissingTranslationBehaviors
    {
        public const string DefaultLanguage = "DefaultLanguage";
        public const string NotFound = "NotFound";
    }


}
