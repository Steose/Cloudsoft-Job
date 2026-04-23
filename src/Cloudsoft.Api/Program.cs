using Cloudsoft.Core.Data;
using Cloudsoft.Core.Options;
using Cloudsoft.Core.Repositories;
using Cloudsoft.Core.Repositories.Interfaces;
using Cloudsoft.Core.Services;
using Cloudsoft.Core.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));
builder.Services.Configure<FeatureFlagsOptions>(builder.Configuration.GetSection(FeatureFlagsOptions.SectionName));

var featureFlags = builder.Configuration
    .GetSection(FeatureFlagsOptions.SectionName)
    .Get<FeatureFlagsOptions>() ?? new FeatureFlagsOptions();

if (featureFlags.UseMongoDb)
{
    builder.Services.AddScoped<IJobPostingRepository, MongoJobPostingRepository>();
    builder.Services.AddScoped<IEmployerRepository, MongoEmployerRepository>();
}
else
{
    builder.Services.AddSingleton<IInMemoryDatabase, InMemoryDatabase>();
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
