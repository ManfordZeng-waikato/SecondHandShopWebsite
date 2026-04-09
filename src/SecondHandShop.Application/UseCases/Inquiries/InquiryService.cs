using System.Security.Cryptography;
using System.Text;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Inquiries;

/// <summary>
/// Public inquiry submission. Anti-spam windows are enforced under a DB transaction with
/// PostgreSQL advisory locks so concurrent requests cannot all pass count checks before insert;
/// Turnstile and IP cooldown remain best-effort layers on top.
/// </summary>
public class InquiryService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IInquiryRepository inquiryRepository,
    ICustomerResolutionService customerResolutionService,
    ITurnstileValidator turnstileValidator,
    IInquiryDispatchSignal dispatchSignal,
    IUnitOfWork unitOfWork,
    IClock clock) : IInquiryService
{
    private static readonly TimeSpan IpProductWindow = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan EmailProductWindow = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan MessageHashWindow = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan IpCooldownDuration = TimeSpan.FromMinutes(5);
    private const int MaxIpProductRequests = 2;
    private const int MaxEmailProductRequests = 1;
    private static readonly HashSet<string> TurnstileServerErrorCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "missing-input-secret",
        "invalid-input-secret",
        "bad-request",
        "internal-error"
    };

    public async Task<Guid> CreateInquiryAsync(CreateInquiryCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedTurnstileToken = Normalize(command.TurnstileToken);
        if (string.IsNullOrWhiteSpace(normalizedTurnstileToken))
        {
            throw new ArgumentException(
                "Please complete the security verification and try again.",
                nameof(command.TurnstileToken));
        }

        var normalizedRequestIpAddress = Normalize(command.RequestIpAddress) ?? "unknown";
        var turnstileValidation = await turnstileValidator.ValidateAsync(
            normalizedTurnstileToken,
            normalizedRequestIpAddress == "unknown" ? null : normalizedRequestIpAddress,
            cancellationToken);

        if (!turnstileValidation.IsSuccess)
        {
            if (ContainsServerErrorCode(turnstileValidation.ErrorCodes))
            {
                throw new TurnstileValidationUnavailableException(
                    "Security verification service is temporarily unavailable. Please try again later.");
            }

            throw new InquiryTurnstileValidationException(
                BuildTurnstileFailureMessage(turnstileValidation.ErrorCodes));
        }

        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{command.ProductId}' was not found.");
        }

        if (product.Status != ProductStatus.Available)
        {
            throw new DomainRuleViolationException("Inquiries can only be submitted for available products.");
        }

        var category = await categoryRepository.GetByIdAsync(product.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            throw new DomainRuleViolationException("Inquiries can only be submitted for products in an active category.");
        }

        var normalizedName = Normalize(command.CustomerName);
        var normalizedEmail = NormalizeEmail(command.Email);
        var normalizedPhoneNumber = Normalize(command.PhoneNumber);
        var messageHash = ComputeMessageHash(command.Message);
        var utcNow = clock.UtcNow;

        await EnsureIpNotInCooldownAsync(normalizedRequestIpAddress, utcNow, cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await inquiryRepository.AcquireAntiSpamConcurrencyLocksAsync(
            normalizedRequestIpAddress,
            command.ProductId,
            normalizedEmail,
            messageHash,
            cancellationToken);

        try
        {
            await EnforceAntiSpamRulesAsync(
                command.ProductId,
                normalizedRequestIpAddress,
                normalizedEmail,
                messageHash,
                utcNow,
                cancellationToken);
        }
        catch (InquiryRateLimitExceededException ex)
        {
            await inquiryRepository.UpsertIpCooldownAsync(
                normalizedRequestIpAddress,
                utcNow.Add(IpCooldownDuration),
                utcNow,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new InquiryRateLimitExceededException(
                $"{ex.Message} This IP is temporarily blocked for 5 minutes.",
                ex);
        }

        var customer = await customerResolutionService.GetOrCreateCustomerAsync(
            normalizedName,
            normalizedEmail,
            normalizedPhoneNumber,
            CustomerSource.Inquiry,
            cancellationToken);

        var inquiry = Inquiry.Create(
            command.ProductId,
            customer.Id,
            normalizedName,
            normalizedEmail,
            normalizedPhoneNumber,
            normalizedRequestIpAddress,
            messageHash,
            command.Message,
            utcNow);

        await inquiryRepository.AddAsync(inquiry, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // The inquiry is persisted in Pending status with NextRetryAt = null so the background
        // InquiryEmailDispatcherService will pick it up immediately. Notify wakes the dispatcher
        // without waiting for its next poll tick.
        dispatchSignal.Notify();

        return inquiry.Id;
    }

    /// <summary>
    /// Sliding-window limits; must run after <see cref="IInquiryRepository.AcquireAntiSpamConcurrencyLocksAsync"/>
    /// in the same transaction so counts and inserts are serialized.
    /// </summary>
    private async Task EnforceAntiSpamRulesAsync(
        Guid productId,
        string requestIpAddress,
        string? email,
        string messageHash,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var ipCount = await inquiryRepository.CountRecentByIpAndProductAsync(
            requestIpAddress,
            productId,
            utcNow - IpProductWindow,
            cancellationToken);
        if (ipCount >= MaxIpProductRequests)
        {
            throw new InquiryRateLimitExceededException("Too many inquiries for this product from the same IP. Please try again in 60 seconds.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailCount = await inquiryRepository.CountRecentByEmailAndProductAsync(
                email,
                productId,
                utcNow - EmailProductWindow,
                cancellationToken);
            if (emailCount >= MaxEmailProductRequests)
            {
                throw new InquiryRateLimitExceededException("This email already sent an inquiry for this product in the last 10 minutes.");
            }
        }

        var hasSameMessageRecently = await inquiryRepository.ExistsRecentByMessageHashAsync(
            messageHash,
            utcNow - MessageHashWindow,
            cancellationToken);
        if (hasSameMessageRecently)
        {
            throw new InquiryRateLimitExceededException("The same message was already submitted in the last 30 minutes.");
        }
    }

    private async Task EnsureIpNotInCooldownAsync(
        string requestIpAddress,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var blockedUntil = await inquiryRepository.GetIpCooldownUntilAsync(requestIpAddress, cancellationToken);
        if (blockedUntil is null || blockedUntil <= utcNow)
        {
            return;
        }

        var retryAfterSeconds = (int)Math.Ceiling((blockedUntil.Value - utcNow).TotalSeconds);
        throw new InquiryRateLimitExceededException(
            $"This IP is temporarily blocked due to repeated limit violations. Try again in {retryAfterSeconds} seconds.");
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeEmail(string? value)
    {
        var normalized = Normalize(value);
        return normalized?.ToLowerInvariant();
    }

    private static string ComputeMessageHash(string message)
    {
        var normalizedMessage = message
            .Trim()
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedMessage));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool ContainsServerErrorCode(IReadOnlyCollection<string> errorCodes)
    {
        return errorCodes.Any(code => TurnstileServerErrorCodes.Contains(code));
    }

    private static string BuildTurnstileFailureMessage(IReadOnlyCollection<string> errorCodes)
    {
        if (errorCodes.Any(code => string.Equals(code, "timeout-or-duplicate", StringComparison.OrdinalIgnoreCase)))
        {
            return "Security verification expired or was already used. Please verify again.";
        }

        if (errorCodes.Any(code =>
                string.Equals(code, "missing-input-response", StringComparison.OrdinalIgnoreCase)
                || string.Equals(code, "invalid-input-response", StringComparison.OrdinalIgnoreCase)))
        {
            return "Security verification is missing or invalid. Please verify and try again.";
        }

        return "Security verification failed. Please verify and try again.";
    }
}
