using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.Messaging;

namespace SecondHandShop.Infrastructure.Services;

public sealed class AdminLoginNotificationDispatcherService(
    IServiceScopeFactory scopeFactory,
    AdminLoginNotificationQueue queue,
    ILogger<AdminLoginNotificationDispatcherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Admin login notification dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await queue.DequeueAsync(stoppingToken);
                await using var scope = scopeFactory.CreateAsyncScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                await emailSender.SendAdminLoginNotificationAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Admin login notification dispatch failed.");
            }
        }

        logger.LogInformation("Admin login notification dispatcher stopped.");
    }
}
