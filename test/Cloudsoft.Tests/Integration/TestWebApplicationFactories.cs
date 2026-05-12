using Cloudsoft.Api;
using Cloudsoft.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Cloudsoft.Tests.Integration;

internal sealed class CloudsoftApiFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    private readonly Dictionary<string, string?> _configuration;
    private readonly EnvironmentVariableScope _environment;

    public CloudsoftApiFactory(Dictionary<string, string?>? configuration = null)
    {
        _configuration = configuration ?? DefaultTestConfiguration;
        _environment = new EnvironmentVariableScope(ToEnvironmentVariables(_configuration));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(_configuration);
        });
    }

    private static readonly Dictionary<string, string?> DefaultTestConfiguration = new()
    {
        ["FeatureFlags:UseMongoDb"] = "false",
        ["FeatureFlags:UseAzureKeyVault"] = "false",
        ["FeatureFlags:UseAzureStorage"] = "false",
        ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
        ["MongoDb:DatabaseName"] = "Cloudsoft",
        ["MongoDb:JobPostingsCollectionName"] = "jobPostings",
        ["MongoDb:EmployersCollectionName"] = "employers",
        ["AzureKeyVault:KeyVaultUri"] = "",
        ["AzureBlob:ContainerUrl"] = "",
        ["ApiAuth:WriteApiKey"] = "test-api-write-key",
        ["AllowedHosts"] = "*"
    };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _environment.Dispose();
    }

    private static Dictionary<string, string> ToEnvironmentVariables(Dictionary<string, string?> configuration)
    {
        return configuration
            .Where(item => item.Value != null)
            .ToDictionary(item => item.Key.Replace(':', '_').Replace("_", "__"), item => item.Value!);
    }
}

internal sealed class CloudsoftWebFactory : WebApplicationFactory<WebAssemblyMarker>
{
    private readonly Dictionary<string, string?> _configuration;
    private readonly EnvironmentVariableScope _environment;

    public CloudsoftWebFactory(Dictionary<string, string?>? configuration = null)
    {
        _configuration = configuration ?? DefaultTestConfiguration;
        _environment = new EnvironmentVariableScope(ToEnvironmentVariables(_configuration));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.Sources.Clear();
            configuration.AddInMemoryCollection(_configuration);
        });
    }

    private static readonly Dictionary<string, string?> DefaultTestConfiguration = new()
    {
        ["FeatureFlags:UseMongoDb"] = "false",
        ["FeatureFlags:UseAzureKeyVault"] = "false",
        ["FeatureFlags:UseAzureStorage"] = "false",
        ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
        ["MongoDb:DatabaseName"] = "Cloudsoft",
        ["MongoDb:JobPostingsCollectionName"] = "jobPostings",
        ["MongoDb:EmployersCollectionName"] = "employers",
        ["AzureKeyVault:KeyVaultUri"] = "",
        ["AzureBlob:ContainerUrl"] = "",
        ["AllowedHosts"] = "*"
    };

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _environment.Dispose();
    }

    private static Dictionary<string, string> ToEnvironmentVariables(Dictionary<string, string?> configuration)
    {
        return configuration
            .Where(item => item.Value != null)
            .ToDictionary(item => item.Key.Replace(':', '_').Replace("_", "__"), item => item.Value!);
    }
}
