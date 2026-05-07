using Cloudsoft.Api;
using Cloudsoft.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Cloudsoft.Tests.Integration;

internal sealed class CloudsoftApiFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    private readonly Dictionary<string, string?> _configuration;

    public CloudsoftApiFactory(Dictionary<string, string?>? configuration = null)
    {
        _configuration = configuration ?? DefaultTestConfiguration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(_configuration);
        });
    }

    private static readonly Dictionary<string, string?> DefaultTestConfiguration = new()
    {
        ["FeatureFlags:UseMongoDb"] = "false",
        ["FeatureFlags:UseAzureKeyVault"] = "false",
        ["AllowedHosts"] = "*"
    };
}

internal sealed class CloudsoftWebFactory : WebApplicationFactory<WebAssemblyMarker>
{
    private readonly Dictionary<string, string?> _configuration;

    public CloudsoftWebFactory(Dictionary<string, string?>? configuration = null)
    {
        _configuration = configuration ?? DefaultTestConfiguration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(_configuration);
        });
    }

    private static readonly Dictionary<string, string?> DefaultTestConfiguration = new()
    {
        ["FeatureFlags:UseMongoDb"] = "false",
        ["FeatureFlags:UseAzureKeyVault"] = "false",
        ["AllowedHosts"] = "*"
    };
}
