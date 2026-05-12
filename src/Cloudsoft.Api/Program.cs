using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Cloudsoft.Api.Authentication;
using Cloudsoft.Core.Data;
using Cloudsoft.Core.Options;
using Cloudsoft.Core.Repositories;
using Cloudsoft.Core.Repositories.Interfaces;
using Cloudsoft.Core.Services;
using Cloudsoft.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

ConfigureKeyVault(builder);

builder.Services.AddControllers();
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection(FeatureFlagsOptions.SectionName));
builder.Services.Configure<AzureKeyVaultOptions>(builder.Configuration.GetSection(AzureKeyVaultOptions.SectionName));
builder.Services.Configure<ApiAuthOptions>(builder.Configuration.GetSection(ApiAuthOptions.SectionName));

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
builder.Services
    .AddAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationDefaults.AuthenticationScheme,
        options => { });
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
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
        .GetSection(AzureKeyVaultOptions.SectionName)
        .Get<AzureKeyVaultOptions>() ?? new AzureKeyVaultOptions();

    if (string.IsNullOrWhiteSpace(keyVaultOptions.KeyVaultUri))
    {
        Console.WriteLine("Azure Key Vault is enabled by flag but AzureKeyVault:KeyVaultUri is missing. Continuing without Key Vault.");
        return;
    }

    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultOptions.KeyVaultUri),
            new DefaultAzureCredential());
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
