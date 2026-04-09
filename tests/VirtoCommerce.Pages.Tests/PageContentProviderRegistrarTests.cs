using System;
using FluentAssertions;
using Moq;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Data.ContentProviders;
using Xunit;

namespace VirtoCommerce.Pages.Tests;

public class PageContentProviderRegistrarTests
{
    [Fact]
    public void GetProviders_NoProvidersRegistered_ReturnsEmptyList()
    {
        var registrar = new PageContentProviderRegistrar();

        var providers = registrar.GetProviders();

        providers.Should().BeEmpty();
    }

    [Fact]
    public void RegisterProvider_SingleProvider_CanBeRetrieved()
    {
        var registrar = new PageContentProviderRegistrar();
        var provider = new Mock<IPageContentProvider>();
        provider.Setup(p => p.ProviderName).Returns("TestProvider");

        registrar.RegisterProvider(() => provider.Object);

        var providers = registrar.GetProviders();
        providers.Should().HaveCount(1);
        providers[0].ProviderName.Should().Be("TestProvider");
    }

    [Fact]
    public void RegisterProvider_MultipleProviders_AllRetrieved()
    {
        var registrar = new PageContentProviderRegistrar();
        var provider1 = new Mock<IPageContentProvider>();
        provider1.Setup(p => p.ProviderName).Returns("Provider1");
        var provider2 = new Mock<IPageContentProvider>();
        provider2.Setup(p => p.ProviderName).Returns("Provider2");

        registrar.RegisterProvider(() => provider1.Object);
        registrar.RegisterProvider(() => provider2.Object);

        var providers = registrar.GetProviders();
        providers.Should().HaveCount(2);
    }

    [Fact]
    public void RegisterProvider_NullFactory_ThrowsArgumentNullException()
    {
        var registrar = new PageContentProviderRegistrar();

        var act = () => registrar.RegisterProvider(null);

        act.Should().Throw<ArgumentNullException>();
    }
}
