using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Pharmacy.Infrastructure.Services;

public sealed class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(30);
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24);

    public TokenCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Cleanup Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing token cleanup.");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }
        
        _logger.LogInformation("Token Cleanup Background Service is stopping.");
    }

    private async Task CleanupTokensAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();

        var cutoffDate = DateTime.UtcNow.Subtract(_retentionPeriod);

        // Delete tokens that have been revoked or expired for longer than the retention period
        var tokensToDelete = await dbContext.RefreshTokens
            .Where(t => (t.RevokedAt != null && t.RevokedAt < cutoffDate) || 
                        (t.ExpiresAt < cutoffDate))
            .ToListAsync(stoppingToken);

        if (tokensToDelete.Count > 0)
        {
            _logger.LogInformation($"Found {tokensToDelete.Count} expired/revoked tokens to clean up.");
            
            // Note: EF Core bulk delete is preferred in EF7+, 
            // but for wider compatibility we can remove range.
            dbContext.RefreshTokens.RemoveRange(tokensToDelete);
            await dbContext.SaveChangesAsync(stoppingToken);
            
            _logger.LogInformation("Token cleanup completed successfully.");
        }
    }
}
