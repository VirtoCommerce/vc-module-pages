using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data.ExportImport;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.SearchModule.Core.Model;
using Xunit;
using Xunit.v3;

namespace VirtoCommerce.Pages.Tests;

public class PagesExportImportTests
{
    private readonly Mock<IPageDocumentSearchService> _searchServiceMock = new();

    [Fact]
    public async Task ExportImport_RoundTrip_PreservesData()
    {
        var pages = new[]
        {
            new PageDocument
            {
                Id = "page1",
                OuterId = "o1",
                StoreId = "store1",
                Title = "Test Page",
                Description = "Description",
                Content = "<p>Content</p>",
                Status = PageDocumentStatus.Published,
                Visibility = PageDocumentVisibility.Public,
                Permalink = "/test",
                CreatedBy = "admin",
                CreatedDate = new DateTime(2024, 7, 3, 0, 0, 0, DateTimeKind.Utc),
                UserGroups = [],
            },
        };

        // Setup search to return pages for export
        _searchServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), false))
            .ReturnsAsync((PageDocumentSearchCriteria criteria, bool _) =>
            {
                var result = new PageDocumentSearchResult
                {
                    TotalCount = criteria.Skip >= pages.Length ? 0 : pages.Length,
                    Results = criteria.Skip >= pages.Length
                        ? []
                        : pages.Skip(criteria.Skip).Take(criteria.Take).ToList(),
                };
                return result;
            });

        // Capture indexed pages during import
        PageDocument[] importedPages = null;
        _searchServiceMock
            .Setup(s => s.IndexDocuments(It.IsAny<PageDocument[]>()))
            .Callback<PageDocument[]>(p => importedPages = p)
            .ReturnsAsync(new IndexingResult { Items = [] });

        var exportImport = new PagesExportImport(_searchServiceMock.Object);
        var cancellationToken = new Mock<ICancellationToken>();

        // Export
        using var stream = new MemoryStream();
        await exportImport.DoExportAsync(stream, _ => { }, cancellationToken.Object);

        // Import
        stream.Position = 0;
        await exportImport.DoImportAsync(stream, _ => { }, cancellationToken.Object);

        // Verify
        importedPages.Should().NotBeNull();
        importedPages.Should().HaveCount(1);
        importedPages[0].Id.Should().Be("page1");
        importedPages[0].Title.Should().Be("Test Page");
        importedPages[0].StoreId.Should().Be("store1");
        importedPages[0].Status.Should().Be(PageDocumentStatus.Published);
    }

    [Fact]
    public async Task Export_EmptyIndex_WritesEmptyArray()
    {
        _searchServiceMock
            .Setup(s => s.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), false))
            .ReturnsAsync(new PageDocumentSearchResult { TotalCount = 0, Results = [] });

        var exportImport = new PagesExportImport(_searchServiceMock.Object);
        var cancellationToken = new Mock<ICancellationToken>();

        using var stream = new MemoryStream();
        await exportImport.DoExportAsync(stream, _ => { }, cancellationToken.Object);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        json.Should().Contain("\"Pages\"");
        json.Should().Contain("[]");
    }
}
