using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IProductImageRepository
{
    Task<IReadOnlyList<ProductImage>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductImage productImage, CancellationToken cancellationToken = default);
}
