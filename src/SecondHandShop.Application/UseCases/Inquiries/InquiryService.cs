using System.Security.Cryptography;
using System.Text;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UseCases.Inquiries;

public class InquiryService(
    IProductRepository productRepository,
    IInquiryRepository inquiryRepository,
    ICustomerRepository customerRepository,
    ITurnstileValidator turnstileValidator,
    IEmailSender emailSender,
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

        var normalizedName = Normalize(command.CustomerName);
        var normalizedEmail = NormalizeEmail(command.Email);
        var normalizedPhoneNumber = Normalize(command.PhoneNumber);
        var messageHash = ComputeMessageHash(command.Message);
        var utcNow = clock.UtcNow;

        await EnsureIpNotInCooldownAsync(normalizedRequestIpAddress, utcNow, cancellationToken);

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
            throw new InquiryRateLimitExceededException(
                $"{ex.Message} This IP is temporarily blocked for 5 minutes.");
        }

        var customer = await ResolveCustomerAsync(
            normalizedName,
            normalizedEmail,
            normalizedPhoneNumber,
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

        try
        {
            var emailMessage = new InquiryEmailMessage(
                inquiry.Id,
                product.Id,
                product.Title,
                product.Slug,
                inquiry.CustomerName,
                inquiry.Email,
                inquiry.PhoneNumber,
                inquiry.Message);

            await emailSender.SendInquiryAsync(emailMessage, cancellationToken);
            inquiry.MarkEmailSent(clock.UtcNow);
        }
        catch (Exception ex)
        {
            inquiry.MarkEmailFailed(ex.Message, clock.UtcNow.AddMinutes(5));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return inquiry.Id;
    }

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

    private async Task<Customer> ResolveCustomerAsync(
        string? name,
        string? email,
        string? phoneNumber,
        CancellationToken cancellationToken)
    {
        Customer? customerByEmail = null;
        Customer? customerByPhoneNumber = null;

        if (!string.IsNullOrWhiteSpace(email))
        {
            customerByEmail = await customerRepository.GetByEmailAsync(email, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            customerByPhoneNumber = await customerRepository.GetByPhoneNumberAsync(phoneNumber, cancellationToken);
        }

        if (customerByEmail is not null && customerByPhoneNumber is not null && customerByEmail.Id != customerByPhoneNumber.Id)
        {
            throw new InvalidOperationException("Email and phone number belong to different customers.");
        }

        var customer = customerByEmail ?? customerByPhoneNumber;
        if (customer is null)
        {
            customer = Customer.Create(name, email, phoneNumber, clock.UtcNow);
            await customerRepository.AddAsync(customer, cancellationToken);
            return customer;
        }

        var mergedName = name ?? customer.Name;
        var mergedEmail = email ?? customer.Email;
        var mergedPhoneNumber = phoneNumber ?? customer.PhoneNumber;
        customer.UpdateContact(mergedName, mergedEmail, mergedPhoneNumber, clock.UtcNow);
        return customer;
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
