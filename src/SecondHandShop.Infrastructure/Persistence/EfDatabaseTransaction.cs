using Microsoft.EntityFrameworkCore.Storage;
using SecondHandShop.Application.Abstractions.Common;

namespace SecondHandShop.Infrastructure.Persistence;

internal sealed class EfDatabaseTransaction(IDbContextTransaction transaction) : IDatabaseTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
