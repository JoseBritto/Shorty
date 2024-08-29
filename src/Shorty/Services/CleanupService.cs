using Microsoft.EntityFrameworkCore;
using Shorty.Data;
using Timer = System.Timers.Timer;

namespace Shorty.Services;

public class CleanupService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CleanupService> _logger;
    private readonly Timer _timer;
    
    public CleanupService(IServiceProvider serviceProvider, ILogger<CleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _timer = new Timer(TimeSpan.FromHours(2));
    }

    public void StartMonitoring(TimeSpan interval)
    {
        _timer.Interval = interval.TotalMilliseconds;
        _timer.AutoReset = true;
        _timer.Elapsed += async (sender, args) => await TriggerCleanupAsync();
        _timer.Start();
        _logger.LogInformation("Database cleanup will be run every {hours} hours", interval.TotalHours);
    }
    
    public async Task TriggerCleanupAsync()
    {
        _timer.Stop();
        _logger.LogInformation("Cleanup triggered");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var toBeDeleted = db.ShortUrls
            .Where(x => x.IsDeleted || 
                        (x.ExpiresAtUtc != null && x.ExpiresAtUtc < DateTime.UtcNow));
        var count = await toBeDeleted.CountAsync();
        if (count != 0)
        {
            _logger.LogInformation("Deleting {Count} short-urls", count);
            await toBeDeleted.ExecuteDeleteAsync(); // No need for save changes. This is immediate
            _logger.LogInformation("Deleted {Count} short-urls", count);
        }
        else
        {
            _logger.LogInformation("Nothing to cleanup");
        }
        
        _timer.Start();
    }
}