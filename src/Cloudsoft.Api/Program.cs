using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Cloudsoft.Core.Data;
using Cloudsoft.Core.Options;
using Cloudsoft.Core.Repositories;
using Cloudsoft.Core.Repositories.Interfaces;
using Cloudsoft.Core.Services;
using Cloudsoft.Core.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

ConfigureKeyVault(builder);

builder.Services.AddControllers();
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection(FeatureFlagsOptions.SectionName));
builder.Services.Configure<KeyVaultOptions>(builder.Configuration.GetSection(KeyVaultOptions.SectionName));

var featureFlags = builder.Configuration
    .GetSection(FeatureFlagsOptions.SectionName)
    .Get<FeatureFlagsOptions>() ?? new FeatureFlagsOptions();

builder.Services.AddSingleton<IInMemoryDatabase, InMemoryDatabase>();

var useMongoDb = featureFlags.UseMongoDb && HasValidMongoConfiguration(builder.Configuration);
if (featureFlags.UseMongoDb && !useMongoDb)
{
    Console.WriteLine("MongoDb is enabled by flag but configuration is incomplete. Falling back to in-memory repositories.");
}

if (useMongoDb)
{
    builder.Services.AddScoped<MongoJobPostingRepository>();
    builder.Services.AddScoped<MongoEmployerRepository>();
    builder.Services.AddScoped<JobPostingRepository>();
    builder.Services.AddScoped<EmployerRepository>();
    builder.Services.AddScoped<IJobPostingRepository, ResilientJobPostingRepository>();
    builder.Services.AddScoped<IEmployerRepository, ResilientEmployerRepository>();
}
else
{
    builder.Services.AddScoped<IJobPostingRepository, JobPostingRepository>();
    builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();
}

builder.Services.AddScoped<IJobPostingService, JobPostingService>();
builder.Services.AddScoped<IEmployerAuthenticationService, EmployerAuthenticationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static void ConfigureKeyVault(WebApplicationBuilder builder)
{
    var featureFlags = builder.Configuration
        .GetSection(FeatureFlagsOptions.SectionName)
        .Get<FeatureFlagsOptions>() ?? new FeatureFlagsOptions();

    if (!featureFlags.UseAzureKeyVault)
    {
        return;
    }

    var keyVaultOptions = builder.Configuration
        .GetSection(KeyVaultOptions.SectionName)
        .Get<KeyVaultOptions>() ?? new KeyVaultOptions();

    if (string.IsNullOrWhiteSpace(keyVaultOptions.VaultUri))
    {
        Console.WriteLine("Azure Key Vault is enabled by flag but KeyVault:VaultUri is missing. Continuing without Key Vault.");
        return;
    }

    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultOptions.VaultUri),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = string.IsNullOrWhiteSpace(keyVaultOptions.ManagedIdentityClientId)
                    ? null
                    : keyVaultOptions.ManagedIdentityClientId
            }));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to load configuration from Azure Key Vault. Continuing without Key Vault. {ex.Message}");
    }
}

static bool HasValidMongoConfiguration(ConfigurationManager configuration)
{
    var options = configuration
        .GetSection(MongoDbOptions.SectionName)
        .Get<MongoDbOptions>() ?? new MongoDbOptions();

    return !string.IsNullOrWhiteSpace(options.ConnectionString)
        && !string.IsNullOrWhiteSpace(options.DatabaseName)
        && !string.IsNullOrWhiteSpace(options.JobPostingsCollectionName)
        && !string.IsNullOrWhiteSpace(options.EmployersCollectionName);
}
