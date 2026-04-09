using System;
using System.Collections.Generic;

namespace VirtoCommerce.Pages.Core.ContentProviders;

public interface IPageContentProviderRegistrar
{
    void RegisterProvider(Func<IPageContentProvider> providerFactory);
    IReadOnlyList<IPageContentProvider> GetProviders();
}
