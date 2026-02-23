using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UseCases.Inquiries;

public class InquiryService(
    IProductRepository productRepository,
    IInquiryRepository inquiryRepository,
    ICustomerRepository customerRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IClock clock) : IInquiryService
{
    public async Task<Guid> CreateInquiryAsync(CreateInquiryCommand command, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new KeyNotFoundException($"Product '{command.ProductId}' was not found.");
        }

        var normalizedName = Normalize(command.CustomerName);
        var normalizedEmail = NormalizeEmail(command.Email);
        var normalizedPhoneNumber = Normalize(command.PhoneNumber);

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
            command.Message,
            clock.UtcNow);

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
}
