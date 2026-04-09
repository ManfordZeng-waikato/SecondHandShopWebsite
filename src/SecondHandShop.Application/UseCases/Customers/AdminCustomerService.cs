using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
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

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
