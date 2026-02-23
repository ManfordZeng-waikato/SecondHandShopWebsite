using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class CustomerRepository(SecondHandShopDbContext dbContext) : ICustomerRepository
{
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await dbContext.Customers.AddAsync(customer, cancellationToken);
    }
}
