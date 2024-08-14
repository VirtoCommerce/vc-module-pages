using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.Pages.Tests;

public abstract class SearchProviderTestsBase
{
    protected abstract ISearchProvider GetSearchProvider();

    protected virtual IList<PageDocument> GetPagesSet1()
    {
        return new List<PageDocument>
            {
                new ()
                {
                    Id = "1",
                    OuterId = "o1",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "<p>Test page</p>",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Published,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = [],
                    StartDate = new DateTime(2024, 7, 10),
                    EndDate = null,
                },
                new ()
                {
                    Id = "2",
                    OuterId = "o2",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Published,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = [],
                    StartDate = null,
                    EndDate = null,
                },
                new ()
                {
                    Id = "3",
                    OuterId = "o3",
                    StoreId = "store2",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Published,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = [ "customers", "registered" ],
                    StartDate = null,
                    EndDate = null,
                }
            };
    }

    protected virtual IList<PageDocument> GetPagesSet2()
    {
        return new List<PageDocument>
            {
                new ()
                {
                    Id = "1",
                    OuterId = "o1",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "<p>Test page</p>",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Published,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = [],
                    StartDate = new DateTime(2024, 7, 10),
                    EndDate = null,
                },
                new ()
                {
                    Id = "2",
                    OuterId = "o2",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Draft,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = [],
                    StartDate = null,
                    EndDate = null,
                },
                new ()
                {
                    Id = "3",
                    OuterId = "o3",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Published,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = [],
                    StartDate = new DateTime(2024, 07, 10),
                    EndDate = new DateTime(2024, 07, 11),
                },
                new ()
                {
                    Id = "4",
                    OuterId = "o4",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Published,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = [],
                    StartDate = null,
                    EndDate = new DateTime(2024, 07, 11),
                },
                new ()
                {
                    Id = "4",
                    OuterId = "o4",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Published,
                    Visibility = PageDocumentVisibility.Public,
                    UserGroups = ["admin", "useradmin"],
                    StartDate = null,
                    EndDate = null,
                },
                new ()
                {
                    Id = "5",
                    OuterId = "o5",
                    StoreId = "store1",
                    CultureName = null,
                    Permalink = "/test-page",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Draft,
                    Visibility = PageDocumentVisibility.Private,
                    UserGroups = [],
                    StartDate = null,
                    EndDate = null,
                },
                new ()
                {
                    Id = "6",
                    OuterId = "o6",
                    StoreId = "store2",
                    CultureName = null,
                    Permalink = "/testpage",
                    Title = "test page",
                    Description = "description",
                    Content = "# Test page",
                    CreatedBy = "admin",
                    CreatedDate = new DateTime(2024, 7, 3),
                    ModifiedBy = null,
                    ModifiedDate = null,
                    Source = null,
                    MimeType = null,
                    Status = PageDocumentStatus.Unpublished,
                    Visibility = PageDocumentVisibility.Private,
                    UserGroups = ["admin", "customer"],
                    StartDate = null,
                    EndDate = null,
                }
            };
    }


    protected virtual ISettingsManager GetSettingsManager()
    {
        var mock = new Mock<ITestSettingsManager>();

        mock.Setup(s => s.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns((string _, string defaultValue) => defaultValue);
        mock.Setup(s => s.GetValue(It.IsAny<string>(), It.IsAny<bool>())).Returns((string _, bool defaultValue) => defaultValue);
        mock.Setup(s => s.GetValue(It.IsAny<string>(), It.IsAny<int>())).Returns((string _, int defaultValue) => defaultValue);
        mock.Setup(s => s.GetObjectSettingAsync(It.IsAny<string>(), null, null))
            .Returns(Task.FromResult(new ObjectSettingEntry()));

        return mock.Object;
    }

    /// <summary>
    /// Allowing to moq extensions methods
    /// </summary>
    public interface ITestSettingsManager : ISettingsManager
    {
        T GetValue<T>(string name, T defaultValue);
        Task<T> GetValueAsync<T>(string name, T defaultValue);
    }

}
