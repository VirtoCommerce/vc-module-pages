//using GraphQL.Server;
//using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using VirtoCommerce.ExperienceApiModule.Core.Extensions;
//using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Pages.Data;
using VirtoCommerce.Pages.Data.Search;
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
        var assemblyMarker = typeof(AssemblyMarker);
        //var graphQlBuilder = new CustomGraphQLBuilder(serviceCollection);
        //graphQlBuilder.AddGraphTypes(assemblyMarker);
        //serviceCollection.AddMediatR(assemblyMarker);
        //serviceCollection.AddAutoMapper(assemblyMarker);
        //serviceCollection.AddSchemaBuilders(assemblyMarker);

        // Override models
        //AbstractTypeFactory<OriginalModel>.OverrideType<OriginalModel, ExtendedModel>().MapToType<ExtendedEntity>();
        //AbstractTypeFactory<OriginalEntity>.OverrideType<OriginalEntity, ExtendedEntity>();

        // Register services
        serviceCollection.AddTransient<IPageDocumentSearchService, PageDocumentSearchService>();

        serviceCollection.AddTransient<PagesSearchRequestBuilder>();

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

    }

    public void Uninstall()
    {
        // Nothing to do here
    }
}
