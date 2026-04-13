using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

public class PageIndexDocumentChangesProviderTests : IDisposable
{
    private readonly Mock<ISettingsManager> _settingsManagerMock = new();
    private readonly MemoryCache _memoryCache = new(Options.Create(new MemoryCacheOptions()));

    private PageIndexDocumentChangesProvider CreateProvider(params IPageContentProvider[] providers)
    {
        var platformCache = new Mock<IPlatformMemoryCache>();
        platformCache.Setup(x => x.GetDefaultCacheEntryOptions()).Returns(new MemoryCacheEntryOptions());
        // Delegate TryGetValue and CreateEntry to real MemoryCache so GetOrCreateExclusiveAsync works
        platformCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns((object key, out object value) => _memoryCache.TryGetValue(key, out value));
        platformCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) => _memoryCache.CreateEntry(key));

        return new PageIndexDocumentChangesProvider(providers, _settingsManagerMock.Object, platformCache.Object);
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetTotalChangesCountAsync_IncrementalSync_SyncDisabled_ReturnsZero()
    {
        SetSyncEnabled(false);
        var startDate = DateTime.UtcNow.AddDays(-1);

        var provider = CreateProvider();
        var count = await provider.GetTotalChangesCountAsync(startDate, null);

        count.Should().Be(0);
    }

    [Fact]
    public async Task GetTotalChangesCountAsync_IncrementalSync_SyncEnabled_ReturnsProviderCount()
    {
        SetSyncEnabled(true);
        var startDate = DateTime.UtcNow.AddDays(-1);

        var contentProvider = CreateContentProvider("TestCMS", supportsReindexation: true, changes: GenerateChanges("TestCMS", 5));

        var provider = CreateProvider(contentProvider.Object);
        var count = await provider.GetTotalChangesCountAsync(startDate, null);

        count.Should().Be(5);
    }

    [Fact]
    public async Task GetTotalChangesCountAsync_FullReindex_IgnoresSyncSetting()
    {
        SetSyncEnabled(false);

        var contentProvider = CreateContentProvider("TestCMS", supportsReindexation: true, changes: GenerateChanges("TestCMS", 10));

        var provider = CreateProvider(contentProvider.Object);
        var count = await provider.GetTotalChangesCountAsync(null, null);

        count.Should().Be(10);
    }

    [Fact]
    public async Task GetTotalChangesCountAsync_FullReindex_ProviderDoesNotSupportReindexation_Throws()
    {
        var contentProvider = CreateContentProvider("LegacyCMS", supportsReindexation: false, changes: []);

        var provider = CreateProvider(contentProvider.Object);

        var act = () => provider.GetTotalChangesCountAsync(null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*LegacyCMS*");
    }

    [Fact]
    public async Task GetChangesAsync_IncrementalSync_SyncDisabled_ReturnsEmpty()
    {
        SetSyncEnabled(false);
        var startDate = DateTime.UtcNow.AddDays(-1);

        var provider = CreateProvider();
        var changes = await provider.GetChangesAsync(startDate, null, 0, 100);

        changes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChangesAsync_AggregatesChangesFromMultipleProviders()
    {
        SetSyncEnabled(true);
        var startDate = DateTime.UtcNow.AddDays(-1);

        var changes1 = new List<IndexDocumentChange>
        {
            new() { DocumentId = "page1", ChangeDate = DateTime.UtcNow, ChangeType = IndexDocumentChangeType.Modified },
        };
        var changes2 = new List<IndexDocumentChange>
        {
            new() { DocumentId = "page2", ChangeDate = DateTime.UtcNow.AddMinutes(-5), ChangeType = IndexDocumentChangeType.Modified },
        };

        var provider1 = CreateContentProvider("CMS1", true, changes1);
        var provider2 = CreateContentProvider("CMS2", true, changes2);

        var provider = CreateProvider(provider1.Object, provider2.Object);
        var result = await provider.GetChangesAsync(startDate, null, 0, 100);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChangesAsync_FullReindex_MultipleProviders_UnsupportedThrows()
    {
        var supported = CreateContentProvider("SupportedCMS", supportsReindexation: true, changes: GenerateChanges("SupportedCMS", 5));
        var unsupported = CreateContentProvider("UnsupportedCMS", supportsReindexation: false, changes: []);

        var provider = CreateProvider(supported.Object, unsupported.Object);

        var act = () => provider.GetChangesAsync(null, null, 0, 100);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*UnsupportedCMS*");
    }

    [Fact]
    public async Task GetChangesAsync_Pagination_ReturnsCorrectSlice()
    {
        SetSyncEnabled(true);
        var now = DateTime.UtcNow;

        var changes = new List<IndexDocumentChange>();
        for (var i = 0; i < 10; i++)
        {
            changes.Add(new IndexDocumentChange
            {
                DocumentId = $"page{i}",
                ChangeDate = now.AddMinutes(-i),
                ChangeType = IndexDocumentChangeType.Modified,
            });
        }

        var contentProvider = CreateContentProvider("CMS", true, changes);

        var provider = CreateProvider(contentProvider.Object);
        var result = await provider.GetChangesAsync(now.AddDays(-1), null, 2, 3);

        result.Should().HaveCount(3);
    }

    private void SetSyncEnabled(bool enabled)
    {
        var settingName = ModuleConstants.Settings.General.ScheduledSyncEnabled.Name;
        var setting = new ObjectSettingEntry { Value = enabled };

        _settingsManagerMock
            .Setup(s => s.GetObjectSettingAsync(settingName, null, null))
            .ReturnsAsync(setting);
    }

    private static List<IndexDocumentChange> GenerateChanges(string providerName, int count)
    {
        return Enumerable.Range(0, count).Select(i => new IndexDocumentChange
        {
            DocumentId = $"{providerName}-page-{i}",
            ChangeDate = DateTime.UtcNow.AddMinutes(-i),
            ChangeType = IndexDocumentChangeType.Modified,
        }).ToList();
    }

    private static Mock<IPageContentProvider> CreateContentProvider(
        string name,
        bool supportsReindexation,
        IList<IndexDocumentChange> changes)
    {
        var mock = new Mock<IPageContentProvider>();
        mock.Setup(p => p.ProviderName).Returns(name);
        mock.Setup(p => p.SupportsReindexation).Returns(supportsReindexation);
        mock.Setup(p => p.SearchChangesAsync(It.IsAny<PageChangesSearchCriteria>()))
            .ReturnsAsync((PageChangesSearchCriteria criteria) => new PageChangesSearchResult
            {
                TotalCount = changes.Count,
                Results = changes.Skip(criteria.Skip).Take(criteria.Take).ToList(),
            });
        return mock;
    }
}
