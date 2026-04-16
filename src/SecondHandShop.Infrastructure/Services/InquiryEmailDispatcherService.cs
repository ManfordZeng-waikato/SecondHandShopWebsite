using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Services;

/// <summary>
/// Background worker that dispatches Pending inquiry emails. Each iteration creates a fresh DI
/// scope so the <see cref="IEmailSender"/> and <see cref="IUnitOfWork"/> (DbContext) it uses are
/// never shared with an HTTP request's lifetime. The loop wakes via <see cref="IInquiryDispatchSignal"/>
/// when new inquiries arrive, and falls back to a periodic poll so retries still fire.
/// </summary>
public sealed class InquiryEmailDispatcherService(
    IServiceScopeFactory scopeFactory,
    IInquiryDispatchSignal dispatchSignal,
    ILogger<InquiryEmailDispatcherService> logger) : BackgroundService
{
    private static readonly TimeSpan PollFallback = TimeSpan.FromMinutes(1);
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Inquiry email dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inquiry dispatcher batch failed; will retry after backoff.");
            }

            await dispatchSignal.WaitForNextAsync(PollFallback, stoppingToken);
        }

        logger.LogInformation("Inquiry email dispatcher stopped.");
    }

    private async Task ProcessPendingBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var inquiryRepository = provider.GetRequiredService<IInquiryRepository>();
        var productRepository = provider.GetRequiredService<IProductRepository>();
        var emailSender = provider.GetRequiredService<IEmailSender>();
        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var clock = provider.GetRequiredService<IClock>();

        var utcNow = clock.UtcNow;
        var pending = await inquiryRepository.ListPendingEmailAsync(utcNow, cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        logger.LogInformation("Processing {Count} pending inquiry email(s).", pending.Count);

        // Batch-fetch all referenced products in a single query to avoid N+1.
        var productIds = pending.Select(i => i.ProductId).Distinct().ToList();
        var productLookup = await productRepository.GetEmailInfoByIdsAsync(productIds, cancellationToken);

        foreach (var inquiry in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await DispatchOneAsync(inquiry, productLookup, emailSender, clock);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchOneAsync(
        Inquiry inquiry,
        IReadOnlyDictionary<Guid, ProductEmailInfoDto> productLookup,
        IEmailSender emailSender,
        IClock clock)
    {
        if (!productLookup.TryGetValue(inquiry.ProductId, out var product))
        {
            // Product was removed after the inquiry was created — nothing we can deliver.
            logger.LogWarning(
                "Skipping inquiry {InquiryId}: product {ProductId} no longer exists; marking Failed.",
                inquiry.Id, inquiry.ProductId);
            inquiry.MarkEmailFailed("Target product no longer exists.", nextRetryAt: null);
            return;
        }

        var message = new InquiryEmailMessage(
            inquiry.Id,
            product.Id,
            product.Title,
            product.Slug,
            inquiry.CustomerName,
            inquiry.Email,
            inquiry.PhoneNumber,
            inquiry.Message);

        try
        {
            await emailSender.SendInquiryAsync(message, CancellationToken.None);
            inquiry.MarkEmailSent(clock.UtcNow);
            logger.LogInformation("Delivered inquiry email. InquiryId={InquiryId}", inquiry.Id);
        }
        catch (Exception ex)
        {
            // EmailSendAttempts is incremented inside the domain methods below, so compare
            // against MaxAttempts using the value that *will* exist after the call.
            var attemptsAfter = inquiry.EmailSendAttempts + 1;
            if (attemptsAfter >= MaxAttempts)
            {
                logger.LogError(ex,
                    "Giving up on inquiry {InquiryId} after {Attempts} attempts.",
                    inquiry.Id, attemptsAfter);
                inquiry.MarkEmailFailed(ex.Message, nextRetryAt: null);
                return;
            }

            var backoff = ComputeBackoff(attemptsAfter);
            logger.LogWarning(ex,
                "Inquiry {InquiryId} send failed (attempt {Attempt}/{Max}); retrying in {BackoffSeconds}s.",
                inquiry.Id, attemptsAfter, MaxAttempts, (int)backoff.TotalSeconds);
            inquiry.RecordTransientFailure(ex.Message, clock.UtcNow.Add(backoff));
        }
    }

    private static TimeSpan ComputeBackoff(int attempts)
    {
        // Exponential backoff capped at 1 hour: 60s, 120s, 240s, 480s, ...
        var seconds = Math.Min(Math.Pow(2, attempts - 1) * 60d, 3600d);
        return TimeSpan.FromSeconds(seconds);
    }
}
