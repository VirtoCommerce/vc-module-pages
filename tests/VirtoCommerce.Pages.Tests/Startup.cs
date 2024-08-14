using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VirtoCommerce.Pages.Tests;

class Startup
{
    public static void ConfigureHost(IHostBuilder hostBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ElasticSearchTests>()
            .AddEnvironmentVariables()
            .Build();

        hostBuilder.ConfigureHostConfiguration(builder => builder.AddConfiguration(configuration));
    }
}
