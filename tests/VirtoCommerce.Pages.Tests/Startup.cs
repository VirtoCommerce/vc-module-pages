using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace VirtoCommerce.Pages.Tests;

class Startup
{
    public static void ConfigureHost(IHostBuilder hostBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ElasticSearch8Tests>()
            .AddEnvironmentVariables()
            .Build();

        hostBuilder.ConfigureHostConfiguration(builder => builder.AddConfiguration(configuration));
    }
}
