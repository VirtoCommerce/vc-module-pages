using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Pages.Core.ContentProviders;

namespace VirtoCommerce.Pages.Data.ContentProviders;

public class PageContentProviderRegistrar : IPageContentProviderRegistrar
{
    private readonly ConcurrentBag<Func<IPageContentProvider>> _providerFactories = [];

    public void RegisterProvider(Func<IPageContentProvider> providerFactory)
    {
        ArgumentNullException.ThrowIfNull(providerFactory);
        _providerFactories.Add(providerFactory);
    }

    public IReadOnlyList<IPageContentProvider> GetProviders()
    {
        return _providerFactories.Select(factory => factory()).ToList();
    }
}
