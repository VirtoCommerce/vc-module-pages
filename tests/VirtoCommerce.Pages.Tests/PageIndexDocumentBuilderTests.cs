using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Converters;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.SearchModule.Core.Model;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

public class PageIndexDocumentBuilderTests
{
    private readonly Mock<IPageDocumentConverter> _converterMock = new();
    private readonly Mock<ILogger<PageIndexDocumentBuilder>> _loggerMock = new();

    private PageIndexDocumentBuilder CreateBuilder(params IPageContentProvider[] providers)
    {
        return new PageIndexDocumentBuilder(providers, _converterMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetDocumentsAsync_NoProviders_ReturnsEmpty()
    {
        var builder = CreateBuilder();
        var result = await builder.GetDocumentsAsync(["page1"]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDocumentsAsync_ProviderReturnsPages_ConvertsToIndexDocuments()
    {
        var page = new PageDocument { Id = "page1", Title = "Test Page", StoreId = "store1" };
        var indexDoc = new IndexDocument("page1");

        var provider = new Mock<IPageContentProvider>();
        provider.Setup(p => p.GetByIdsAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync([page]);

        _converterMock.Setup(c => c.ToIndexDocument(page)).Returns(indexDoc);

        var builder = CreateBuilder(provider.Object);
        var result = await builder.GetDocumentsAsync(["page1"]);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("page1");
    }

    [Fact]
    public async Task GetDocumentsAsync_MultipleProviders_AggregatesResults()
    {
        var page1 = new PageDocument { Id = "page1", StoreId = "store1" };
        var page2 = new PageDocument { Id = "page2", StoreId = "store1" };

        var provider1 = new Mock<IPageContentProvider>();
        provider1.Setup(p => p.ProviderName).Returns("CMS1");
        provider1.Setup(p => p.GetByIdsAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync([page1]);

        var provider2 = new Mock<IPageContentProvider>();
        provider2.Setup(p => p.ProviderName).Returns("CMS2");
        provider2.Setup(p => p.GetByIdsAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync([page2]);

        _converterMock.Setup(c => c.ToIndexDocument(It.IsAny<PageDocument>()))
            .Returns((PageDocument p) => new IndexDocument(p.Id));

        var builder = CreateBuilder(provider1.Object, provider2.Object);
        var result = await builder.GetDocumentsAsync(["page1", "page2"]);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDocumentsAsync_ProviderThrows_LogsErrorAndContinues()
    {
        var page = new PageDocument { Id = "page2", StoreId = "store1" };

        var failingProvider = new Mock<IPageContentProvider>();
        failingProvider.Setup(p => p.ProviderName).Returns("FailingCMS");
        failingProvider.Setup(p => p.GetByIdsAsync(It.IsAny<IList<string>>()))
            .ThrowsAsync(new Exception("Connection failed"));

        var workingProvider = new Mock<IPageContentProvider>();
        workingProvider.Setup(p => p.ProviderName).Returns("WorkingCMS");
        workingProvider.Setup(p => p.GetByIdsAsync(It.IsAny<IList<string>>()))
            .ReturnsAsync([page]);

        _converterMock.Setup(c => c.ToIndexDocument(page)).Returns(new IndexDocument("page2"));

        var builder = CreateBuilder(failingProvider.Object, workingProvider.Object);
        var result = await builder.GetDocumentsAsync(["page1", "page2"]);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("page2");
    }
}
