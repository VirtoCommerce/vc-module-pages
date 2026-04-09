using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Converters;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data.ContentProviders;
using VirtoCommerce.Pages.Data.Converters;
using VirtoCommerce.Pages.Data.ExportImport;
using VirtoCommerce.Pages.Data.Handlers;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.ExportImport;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.Seo.Core.Services;
using VirtoCommerce.StoreModule.Core.Model;

namespace VirtoCommerce.Pages.Web;

public class Module : IModule, IExportSupport, IImportSupport, IHasConfiguration
{
    private IApplicationBuilder _appBuilder;

    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        // Register services
        serviceCollection.AddTransient<IPageDocumentSearchService, PageDocumentSearchService>();
        serviceCollection.AddTransient<IPageDocumentConverter, PageDocumentConverter>();
        serviceCollection.AddTransient<ISeoResolver, PageDocumentSeoResolver>();
        serviceCollection.AddTransient<PageSearchRequestBuilder>();
        serviceCollection.AddTransient<PageDocumentConverter>();
        serviceCollection.AddTransient<PageChangedHandler>();

        // Content providers
        serviceCollection.AddSingleton<IPageContentProviderRegistrar, PageContentProviderRegistrar>();

        // Index rebuild
        serviceCollection.AddTransient<PageIndexDocumentBuilder>();
        serviceCollection.AddTransient<PageIndexDocumentChangesProvider>();

        serviceCollection.AddSingleton(provider => new IndexDocumentConfiguration
        {
            DocumentType = ModuleConstants.PageDocumentType,
            DocumentSource = new IndexDocumentSource
            {
                ChangesProvider = provider.GetService<PageIndexDocumentChangesProvider>(),
                DocumentBuilder = provider.GetService<PageIndexDocumentBuilder>(),
            },
        });

        // Export/Import
        serviceCollection.AddTransient<PagesExportImport>();
    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        _appBuilder = appBuilder;
        var serviceProvider = appBuilder.ApplicationServices;

        // Register permissions
        var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
        permissionsRegistrar.RegisterPermissions(ModuleInfo.Id,
            ModuleConstants.Security.ModuleGroup,
            ModuleConstants.Security.Permissions.AllPermissions);

        // Register settings
        var settingsRegistrar = serviceProvider.GetRequiredService<ISettingsRegistrar>();
        settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

        var searchRequestBuilderRegistrar = appBuilder.ApplicationServices.GetService<ISearchRequestBuilderRegistrar>();
        searchRequestBuilderRegistrar.Register(ModuleConstants.PageDocumentType, appBuilder.ApplicationServices.GetService<PageSearchRequestBuilder>);

        // Register store level settings
        settingsRegistrar.RegisterSettingsForType(ModuleConstants.Settings.StoreLevelSettings, nameof(Store));

        appBuilder.RegisterEventHandler<PagesDomainEvent, PageChangedHandler>();
    }

    public void Uninstall()
    {
        // Nothing to do here
    }

    public Task ExportAsync(Stream outStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        return _appBuilder.ApplicationServices.GetRequiredService<PagesExportImport>()
            .DoExportAsync(outStream, progressCallback, cancellationToken);
    }

    public Task ImportAsync(Stream inputStream, ExportImportOptions options, Action<ExportImportProgressInfo> progressCallback, ICancellationToken cancellationToken)
    {
        return _appBuilder.ApplicationServices.GetRequiredService<PagesExportImport>()
            .DoImportAsync(inputStream, progressCallback, cancellationToken);
    }
}
