using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Infrastructure.Background;

public class ExpirySweeper : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpirySweeper> _logger;

    public ExpirySweeper(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ExpirySweeper> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue("EXPIRY_SWEEP_INTERVAL_SECONDS", 5);
        if (intervalSeconds <= 0) intervalSeconds = 5;

        _logger.LogInformation("Starting ExpirySweeper background service with interval {IntervalSeconds}s.", intervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var affectedRows = await context.Database.ExecuteSqlRawAsync(
                    @"UPDATE holds 
                      SET status = 'EXPIRED', updated_at = clock_timestamp() 
                      WHERE status = 'HELD' AND expires_at <= clock_timestamp();",
                    stoppingToken);

                if (affectedRows > 0)
                {
                    _logger.LogInformation("ExpirySweeper marked {Count} expired hold(s) as EXPIRED.", affectedRows);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during ExpirySweeper execution cycle.");
            }
        }

        _logger.LogInformation("ExpirySweeper background service stopped.");
    }
}
