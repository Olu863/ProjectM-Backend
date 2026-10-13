using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectM.Services.Services;

namespace ProjectM.API.Services
{
    public class TokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval;

        public TokenCleanupService(
            IServiceProvider serviceProvider,
            ILogger<TokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            // Run cleanup every 24 hours
            _cleanupInterval = TimeSpan.FromHours(24);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DoWorkAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up tokens.");
                }

                // Wait for the next cleanup cycle
                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Token Cleanup Service is stopping.");
        }

        private async Task DoWorkAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service is performing cleanup at: {Time}", DateTimeOffset.UtcNow);

            using (var scope = _serviceProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                try
                {
                    var deletedCount = await userService.CleanupExpiredTokensAsync();

                    if (deletedCount > 0)
                    {
                        _logger.LogInformation(
                            "Token Cleanup Service cleaned up {Count} expired/old tokens at {Time}",
                            deletedCount,
                            DateTimeOffset.UtcNow);
                    }
                    else
                    {
                        _logger.LogInformation("Token Cleanup Service: No tokens to clean up at {Time}", DateTimeOffset.UtcNow);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in TokenCleanupService while cleaning tokens");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service is stopping gracefully.");
            await base.StopAsync(stoppingToken);
        }
    }
}
