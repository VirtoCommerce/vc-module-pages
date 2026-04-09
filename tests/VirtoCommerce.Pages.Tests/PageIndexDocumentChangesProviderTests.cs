using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.Platform.Core.Caching;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

public class PageIndexDocumentChangesProviderTests : IDisposable
{
    private readonly Mock<IPageContentProviderRegistrar> _registrarMock = new();
    private readonly Mock<ISettingsManager> _settingsManagerMock = new();
    private readonly MemoryCache _memoryCache = new(Options.Create(new MemoryCacheOptions()));

    private PageIndexDocumentChangesProvider CreateProvider()
    {
        var platformCache = new Mock<IPlatformMemoryCache>();
        platformCache.Setup(x => x.GetDefaultCacheEntryOptions()).Returns(new MemoryCacheEntryOptions());
        // Delegate TryGetValue and CreateEntry to real MemoryCache so GetOrCreateExclusiveAsync works
        platformCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny))
            .Returns((object key, out object value) => _memoryCache.TryGetValue(key, out value));
        platformCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns((object key) => _memoryCache.CreateEntry(key));

        return new PageIndexDocumentChangesProvider(_registrarMock.Object, _settingsManagerMock.Object, platformCache.Object);
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

        var contentProvider = CreateContentProvider("TestCMS", supportsReindexation: true, totalChanges: 5);
        _registrarMock.Setup(r => r.GetProviders()).Returns([contentProvider.Object]);

        var provider = CreateProvider();
        var count = await provider.GetTotalChangesCountAsync(startDate, null);

        count.Should().Be(5);
    }

    [Fact]
    public async Task GetTotalChangesCountAsync_FullReindex_IgnoresSyncSetting()
    {
        SetSyncEnabled(false);

        var contentProvider = CreateContentProvider("TestCMS", supportsReindexation: true, totalChanges: 10);
        _registrarMock.Setup(r => r.GetProviders()).Returns([contentProvider.Object]);

        var provider = CreateProvider();
        var count = await provider.GetTotalChangesCountAsync(null, null);

        count.Should().Be(10);
    }

    [Fact]
    public async Task GetTotalChangesCountAsync_FullReindex_ProviderDoesNotSupportReindexation_Throws()
    {
        var contentProvider = CreateContentProvider("LegacyCMS", supportsReindexation: false, totalChanges: 0);
        _registrarMock.Setup(r => r.GetProviders()).Returns([contentProvider.Object]);

        var provider = CreateProvider();

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

        var provider1 = CreateContentProvider("CMS1", true, 1, changes1);
        var provider2 = CreateContentProvider("CMS2", true, 1, changes2);
        _registrarMock.Setup(r => r.GetProviders()).Returns([provider1.Object, provider2.Object]);

        var provider = CreateProvider();
        var result = await provider.GetChangesAsync(startDate, null, 0, 100);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChangesAsync_FullReindex_MultipleProviders_UnsupportedThrows()
    {
        var supported = CreateContentProvider("SupportedCMS", supportsReindexation: true, totalChanges: 5);
        var unsupported = CreateContentProvider("UnsupportedCMS", supportsReindexation: false, totalChanges: 0);
        _registrarMock.Setup(r => r.GetProviders()).Returns([supported.Object, unsupported.Object]);

        var provider = CreateProvider();

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

        var contentProvider = CreateContentProvider("CMS", true, 10, changes);
        _registrarMock.Setup(r => r.GetProviders()).Returns([contentProvider.Object]);

        var provider = CreateProvider();
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

    private static Mock<IPageContentProvider> CreateContentProvider(
        string name,
        bool supportsReindexation,
        long totalChanges,
        IList<IndexDocumentChange> changes = null)
    {
        var mock = new Mock<IPageContentProvider>();
        mock.Setup(p => p.ProviderName).Returns(name);
        mock.Setup(p => p.SupportsReindexation).Returns(supportsReindexation);
        mock.Setup(p => p.GetTotalChangesCountAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(totalChanges);
        mock.Setup(p => p.GetChangesAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<long>(), It.IsAny<long>()))
            .ReturnsAsync(changes ?? []);
        return mock;
    }
}
