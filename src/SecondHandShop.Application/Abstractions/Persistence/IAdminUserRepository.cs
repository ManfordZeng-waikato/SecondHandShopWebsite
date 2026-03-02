using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task AddAsync(AdminUser user, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
