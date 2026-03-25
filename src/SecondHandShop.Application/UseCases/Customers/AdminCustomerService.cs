using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
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

        var normalizedPhone = Normalize(request.PhoneNumber);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var existingByPhone = await customerRepository.GetByPhoneNumberAsync(normalizedPhone, cancellationToken);
            if (existingByPhone is not null && existingByPhone.Id != customerId)
            {
                throw new InvalidOperationException("The phone number is already used by another customer.");
            }
        }

        var targetStatus = request.Status ?? customer.Status;
        var targetNotes = request.Notes ?? customer.Notes;

        // Keep email immutable in admin edit flow to avoid accidental identity mismatch.
        customer.UpdateByAdmin(
            request.Name,
            normalizedPhone,
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
