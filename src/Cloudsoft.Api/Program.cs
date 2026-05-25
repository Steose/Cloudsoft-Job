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

// OpenAPI / Swagger surface.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "API key needed to access protected endpoints. Use: X-API-Key: <key>",
        Name = "X-API-Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "ApiKey"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

// Swagger is enabled in BOTH Development AND Production for this course.
// See the Concept Deep Dive below for the trade-off.
app.UseSwagger();
app.UseSwaggerUI();

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
