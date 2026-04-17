using CloudsoftJob.Core.Services;
using CloudsoftJob.Core.Services.Interfaces;
using CloudsoftJob.Core.Data;
using CloudsoftJob.Core.Options;
using CloudsoftJob.Core.Repositories;
using CloudsoftJob.Core.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
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
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
