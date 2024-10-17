//using GraphQL.Server;
//using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CoreModule.Core.Seo;

//using VirtoCommerce.ExperienceApiModule.Core.Extensions;
//using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Events;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data.Handlers;
using VirtoCommerce.Pages.Data.Search;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Pages.Web;

public class Module : IModule, IHasConfiguration
{
    public ManifestModuleInfo ModuleInfo { get; set; }
    public IConfiguration Configuration { get; set; }

    public void Initialize(IServiceCollection serviceCollection)
    {
        // Register services
        serviceCollection.AddTransient<IPageDocumentSearchService, PageDocumentSearchService>();
        serviceCollection.AddTransient<ISeoBySlugResolver, BuilderIOSlugResolver>();
        serviceCollection.AddTransient<PagesSearchRequestBuilder>();
        serviceCollection.AddTransient<PageChangedHandler>();

    }

    public void PostInitialize(IApplicationBuilder appBuilder)
    {
        var serviceProvider = appBuilder.ApplicationServices;

        // Register permissions
        var permissionsRegistrar = serviceProvider.GetRequiredService<IPermissionsRegistrar>();
        permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "Pages", ModuleConstants.Security.Permissions.AllPermissions);

        // Register settings
        var settingsRegistrar = appBuilder.ApplicationServices.GetRequiredService<ISettingsRegistrar>();
        settingsRegistrar.RegisterSettings(ModuleConstants.Settings.AllSettings, ModuleInfo.Id);

        appBuilder.RegisterEventHandler<PagesDomainEvent, PageChangedHandler>();
    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
