using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Cloudsoft.Core.Services;
using Cloudsoft.Core.Services.Interfaces;
using Cloudsoft.Core.Data;
using Cloudsoft.Core.Options;
using Cloudsoft.Core.Repositories;
using Cloudsoft.Core.Repositories.Interfaces;
using Cloudsoft.Core.Storage;
using Cloudsoft.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = ResolveContentRoot(),
    EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Production,
    WebRootPath = "wwwroot"
});

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

if (args.Length > 0)
{
    builder.Configuration.AddCommandLine(args);
}

builder.Logging.AddConsole();
builder.WebHost.UseKestrel();

var configuredUrls = builder.Configuration["urls"] ?? builder.Configuration["ASPNETCORE_URLS"];
if (!string.IsNullOrWhiteSpace(configuredUrls))
{
    builder.WebHost.UseUrls(configuredUrls);
}

//ConfigureKeyVault(builder);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection(FeatureFlagsOptions.SectionName));
builder.Services.Configure<AzureBlobOptions>(builder.Configuration.GetSection(AzureBlobOptions.SectionName));
builder.Services.Configure<AzureKeyVaultOptions>(builder.Configuration.GetSection(AzureKeyVaultOptions.SectionName));

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
    builder.Services.AddScoped<MongoJobApplicationRepository>();
    builder.Services.AddScoped<MongoEmployerRepository>();
    builder.Services.AddScoped<JobPostingRepository>();
    builder.Services.AddScoped<JobApplicationRepository>();
    builder.Services.AddScoped<EmployerRepository>();
    builder.Services.AddScoped<IJobPostingRepository, ResilientJobPostingRepository>();
    builder.Services.AddScoped<IJobApplicationRepository, ResilientJobApplicationRepository>();
    builder.Services.AddScoped<IEmployerRepository, ResilientEmployerRepository>();

    Console.WriteLine("Using MongoDB repository");

}
else
{
    builder.Services.AddScoped<IJobPostingRepository, JobPostingRepository>();
    builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
    builder.Services.AddScoped<IEmployerRepository, EmployerRepository>();

    Console.WriteLine("Using in-memory repository");

}

builder.Services.AddScoped<IJobPostingService, JobPostingService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddSingleton<ICountryLookupService, CountryLookupService>();
builder.Services.AddScoped<IEmployerAuthenticationService, EmployerAuthenticationService>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddHttpContextAccessor();
// Check if Azure Storage should be used
bool useAzureStorage = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureStorage");

if (useAzureStorage)
{
    // Register Azure Blob Storage image service for production
    builder.Services.AddSingleton<IImageService, AzureBlobImageService>();
    builder.Services.AddSingleton<ICvStorageService, LocalCvStorageService>();
    Console.WriteLine("Using Azure Blob Storage for images");
}
else
{
    // Register local image service for development
    builder.Services.AddSingleton<IImageService, LocalImageService>();
    builder.Services.AddSingleton<ICvStorageService, LocalCvStorageService>();
    Console.WriteLine("Using local storage for images");
}
// Check if Azure Key Vault should be used
bool useAzureKeyVault = builder.Configuration.GetValue<bool>("FeatureFlags:UseAzureKeyVault");

if (useAzureKeyVault)
{
    // Get Key Vault URI from configuration
    var keyVaultOptions = builder.Configuration
        .GetSection(AzureKeyVaultOptions.SectionName)
        .Get<AzureKeyVaultOptions>() ?? new AzureKeyVaultOptions();
    var keyVaultUri = keyVaultOptions?.KeyVaultUri;

    // Register Azure Key Vault as configuration provider
    if (string.IsNullOrEmpty(keyVaultUri))
    {
        throw new InvalidOperationException("Key Vault URI is not configured.");
    }

    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());

    Console.WriteLine("Using Azure Key Vault for configuration");
}





var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

//  static void ConfigureKeyVault(WebApplicationBuilder builder)
//  {
    //  var featureFlags = builder.Configuration
        //  .GetSection(FeatureFlagsOptions.SectionName)
        //  .Get<FeatureFlagsOptions>() ?? new FeatureFlagsOptions();
//  
    //  if (!featureFlags.UseAzureKeyVault)
    //  {
        //  return;
    //  }
//  
    //  var keyVaultOptions = builder.Configuration
        //  .GetSection(AzureKeyVaultOptions.SectionName)
        //  .Get<KeyVaultOptions>() ?? new KeyVaultOptions();
//  
    //  if (string.IsNullOrWhiteSpace(keyVaultOptions.VaultUri))
    //  {
        //  Console.WriteLine("Azure Key Vault is enabled by flag but KeyVault:VaultUri is missing. Continuing without Key Vault.");
        //  return;
    //  }
//  
    //  try
    //  {
        //  builder.Configuration.AddAzureKeyVault(
            //  new Uri(keyVaultOptions.VaultUri),
            //  new DefaultAzureCredential(new DefaultAzureCredentialOptions
            //  {
                //  ManagedIdentityClientId = string.IsNullOrWhiteSpace(keyVaultOptions.ManagedIdentityClientId)
                    //  ? null
                    //  : keyVaultOptions.ManagedIdentityClientId
            //  }));
    //  }
    //  catch (Exception ex)
    //  {
        //  Console.WriteLine($"Failed to load configuration from Azure Key Vault. Continuing without Key Vault. {ex.Message}");
    //  }
//  }

static bool HasValidMongoConfiguration(ConfigurationManager configuration)
{
    var options = configuration
        .GetSection(MongoDbOptions.SectionName)
        .Get<MongoDbOptions>() ?? new MongoDbOptions();

    return !string.IsNullOrWhiteSpace(options.ConnectionString)
        && !string.IsNullOrWhiteSpace(options.DatabaseName)
        && !string.IsNullOrWhiteSpace(options.JobPostingsCollectionName)
        && !string.IsNullOrWhiteSpace(options.EmployersCollectionName)
        && !string.IsNullOrWhiteSpace(options.JobApplicationsCollectionName);
}

static string ResolveContentRoot()
{
    var currentDirectory = Directory.GetCurrentDirectory();
    if (Directory.Exists(Path.Combine(currentDirectory, "Views"))
        && Directory.Exists(Path.Combine(currentDirectory, "wwwroot")))
    {
        return currentDirectory;
    }

    var webProjectDirectory = Path.Combine(currentDirectory, "src", "Cloudsoft.Web");
    if (Directory.Exists(Path.Combine(webProjectDirectory, "Views"))
        && Directory.Exists(Path.Combine(webProjectDirectory, "wwwroot")))
    {
        return webProjectDirectory;
    }

    return currentDirectory;
}
