using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Customers;

public class AdminCustomerService(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IAdminCustomerService
{
    public async Task UpdateCustomerAsync(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new KeyNotFoundException($"Customer '{customerId}' was not found.");

        var targetName = request.Name ?? customer.Name;
        var targetPhone = Normalize(request.PhoneNumber) ?? customer.PhoneNumber;
        var targetStatus = request.Status ?? customer.Status;
        var targetNotes = request.Notes ?? customer.Notes;

        if (!string.IsNullOrWhiteSpace(targetPhone) && targetPhone != customer.PhoneNumber)
        {
            var existingByPhone = await customerRepository.GetByPhoneNumberAsync(targetPhone, cancellationToken);
            if (existingByPhone is not null && existingByPhone.Id != customerId)
            {
                throw new ConflictException("The phone number is already used by another customer.");
            }
        }

        customer.UpdateByAdmin(
            targetName,
            targetPhone,
            targetStatus,
            targetNotes,
            clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Normalize(request.Email)?.ToLowerInvariant();
        var normalizedPhone = Normalize(request.PhoneNumber);

        if (normalizedEmail is not null)
        {
            var existingByEmail = await customerRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
            if (existingByEmail is not null)
            {
                throw new CustomerConflictException(
                    existingByEmail.Id,
                    "email",
                    "A customer with this email already exists.");
            }
        }

        if (normalizedPhone is not null)
        {
            var existingByPhone = await customerRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken);
            if (existingByPhone is not null)
            {
                throw new CustomerConflictException(
                    existingByPhone.Id,
                    "phoneNumber",
                    "A customer with this phone number already exists.");
            }
        }

        var utcNow = clock.UtcNow;
        var customer = Customer.Create(
            Normalize(request.Name),
            normalizedEmail,
            normalizedPhone,
            CustomerSource.Manual,
            utcNow);

        var desiredStatus = request.Status ?? CustomerStatus.New;
        var desiredNotes = Normalize(request.Notes);
        if (desiredStatus != CustomerStatus.New || desiredNotes is not null)
        {
            customer.UpdateByAdmin(
                customer.Name,
                customer.PhoneNumber,
                desiredStatus,
                desiredNotes,
                utcNow);
        }

        await customerRepository.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
