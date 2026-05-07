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
        using var environment = new EnvironmentVariableScope(MongoEnabledEnvironment());
        using var factory = new CloudsoftWebFactory();
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();

        Assert.IsType<ResilientJobPostingRepository>(repository);
    }

    [Fact]
    public void ApiApp_UsesResilientRepositoriesWhenMongoDbIsEnabledAndConfigured()
    {
        using var environment = new EnvironmentVariableScope(MongoEnabledEnvironment());
        using var factory = new CloudsoftApiFactory();
        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IJobPostingRepository>();

        Assert.IsType<ResilientJobPostingRepository>(repository);
    }

    private static Dictionary<string, string> MongoEnabledEnvironment()
    {
        return new Dictionary<string, string>
        {
            ["FeatureFlags__UseMongoDb"] = "true",
            ["FeatureFlags__UseAzureKeyVault"] = "false",
            ["MongoDb__ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDb__DatabaseName"] = "cloudsoft-tests",
            ["MongoDb__JobPostingsCollectionName"] = "jobPostings",
            ["MongoDb__EmployersCollectionName"] = "employers"
        };
    }
}
