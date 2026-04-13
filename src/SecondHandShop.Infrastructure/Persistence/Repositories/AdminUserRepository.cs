using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class AdminUserRepository(SecondHandShopDbContext dbContext) : IAdminUserRepository
{
    public async Task<AdminUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.Trim();

        return await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.UserName == normalizedUserName, cancellationToken);
    }

    public async Task<AdminUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        await dbContext.AdminUsers.AddAsync(user, cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AdminUsers.AnyAsync(cancellationToken);
    }
}
