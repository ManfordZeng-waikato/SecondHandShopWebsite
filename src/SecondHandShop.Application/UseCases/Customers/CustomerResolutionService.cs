using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Customers;

public class CustomerResolutionService(
    ICustomerRepository customerRepository,
    IClock clock) : ICustomerResolutionService
{
    public async Task<Customer> GetOrCreateCustomerAsync(
        string? name,
        string? email,
        string? phoneNumber,
        CustomerSource source,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        var normalizedPhone = Normalize(phoneNumber);
        var normalizedName = Normalize(name);

        Customer? customerByEmail = null;
        Customer? customerByPhone = null;

        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            customerByEmail = await customerRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            customerByPhone = await customerRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken);
        }

        // If both match different customers, that's a conflict
        if (customerByEmail is not null && customerByPhone is not null
            && customerByEmail.Id != customerByPhone.Id)
        {
            throw new InvalidOperationException(
                "Email and phone number belong to different customers.");
        }

        var existing = customerByEmail ?? customerByPhone;
        if (existing is not null)
        {
            // Cautious merge: fill blanks, don't overwrite existing values
            existing.MergeContact(normalizedName, normalizedEmail, normalizedPhone, clock.UtcNow);
            return existing;
        }

        // Create new customer
        var customer = Customer.Create(normalizedName, normalizedEmail, normalizedPhone, source, clock.UtcNow);
        await customerRepository.AddAsync(customer, cancellationToken);
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
}
