using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramBotForGitHub.Services.Interfaces;

namespace TelegramBotForGitHub.Services;

public class AuthStateCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuthStateCleanupService> _logger;

    public AuthStateCleanupService(IServiceProvider serviceProvider, ILogger<AuthStateCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var gitHubService = scope.ServiceProvider.GetRequiredService<IGitHubService>();
                gitHubService.CleanExpiredStates();
                
                _logger.LogDebug("Cleaned expired auth states");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning expired auth states");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}