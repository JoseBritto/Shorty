using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shorty.Data;
using Shorty.Helpers;
using Shorty.Services;

[assembly: InternalsVisibleTo("Shorty.Tests")]

var builder = WebApplication.CreateBuilder(args);

var configTask = ConfigManager.LoadConfigFromDiskAsync();
var config = await configTask;
if(config == null)
{
    Console.Error.WriteLine("Critical Error: Failed to read/parse config file!");
    Environment.Exit(Constants.ExitCodes.CONFIG_ERROR);
}

if (!ConfigValidator.ValidateAll(config))
{
    Console.Error.WriteLine("Critical Error: Config validation failed. Reverting to defaults for this session!");
    config = new ShortyConfig();
}

var configMan = new ConfigManager(config);

builder.Services.AddSingleton(configMan);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("Data Source=shorty.db");
});

builder.Services.AddSingleton<CleanupService>();

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "restricted", options =>
    {
        options.PermitLimit = 20;
        options.Window = TimeSpan.FromMinutes(30);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 1;
    }));

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "unrestricted", options =>
    {
        options.PermitLimit = 60;
        options.Window = TimeSpan.FromMinutes(60);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 1;
    }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller}/{action}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseRateLimiter();

/*
ConfigHelper.EnsureUrlSettingsConfigValidity(app.Configuration);
*/

app.UseStatusCodePagesWithReExecute("/Home/Error{0}");

var cleanupService = app.Services.GetRequiredService<CleanupService>();
cleanupService.StartMonitoring(TimeSpan.FromHours(configMan.Config.DatabaseCleanupScheduleHours));

app.Run();