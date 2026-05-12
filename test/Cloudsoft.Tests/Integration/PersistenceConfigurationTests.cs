using Cloudsoft.Core.Repositories;
using Cloudsoft.Core.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudsoft.Tests.Integration;

public class PersistenceConfigurationTests
{
    [Fact]
    public void WebApp_UsesInMemoryRepositoriesWhenMongoDbIsDisabled()
    {
        using var factory = new CloudsoftWebFactory();
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();

        Assert.IsType<JobPostingRepository>(repository);
    }

    [Fact]
    public void ApiApp_UsesInMemoryRepositoriesWhenMongoDbIsDisabled()
    {
        using var factory = new CloudsoftApiFactory();
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();

        Assert.IsType<JobPostingRepository>(repository);
    }

    [Fact]
    public void WebApp_UsesResilientRepositoriesWhenMongoDbIsEnabledAndConfigured()
    {
        using var factory = new CloudsoftWebFactory(MongoEnabledConfiguration());
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();

        Assert.IsType<ResilientJobPostingRepository>(repository);
    }

    [Fact]
    public void ApiApp_UsesResilientRepositoriesWhenMongoDbIsEnabledAndConfigured()
    {
        using var factory = new CloudsoftApiFactory(MongoEnabledConfiguration());
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();

        Assert.IsType<ResilientJobPostingRepository>(repository);
    }

    private static Dictionary<string, string?> MongoEnabledConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["FeatureFlags:UseMongoDb"] = "true",
            ["FeatureFlags:UseAzureKeyVault"] = "false",
            ["FeatureFlags:UseAzureStorage"] = "false",
            ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDb:DatabaseName"] = "cloudsoft-tests",
            ["MongoDb:JobPostingsCollectionName"] = "jobPostings",
            ["MongoDb:EmployersCollectionName"] = "employers",
            ["AzureKeyVault:KeyVaultUri"] = "",
            ["AzureBlob:ContainerUrl"] = "",
            ["ApiAuth:WriteApiKey"] = "test-api-write-key",
            ["AllowedHosts"] = "*"
        };
    }
}
