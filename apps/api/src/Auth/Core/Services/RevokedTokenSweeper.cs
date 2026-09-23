using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Xpeak.Api.Auth.Core;

/// <summary>
/// Once a day, delete <c>RevokedToken</c> rows whose token has
/// already expired -- keeping the table bounded. Runs as a hosted
/// service; sleeps 24h between runs, wakes on shutdown.
/// </summary>
public sealed class RevokedTokenSweeper(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RevokedTokenSweeper> logger)
    : BackgroundService
{
    private static readonly TimeSpan Period = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<RevokedTokenRepository>();
                var deleted = await repo.DeleteExpiredAsync(timeProvider.GetUtcNow(), stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("Swept {Count} expired revoked tokens.", deleted);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Revoked token sweep failed; will retry next cycle.");
            }

            try
            {
                await Task.Delay(Period, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
