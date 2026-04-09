namespace SecondHandShop.Application.Abstractions.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction. The caller must <see cref="IDatabaseTransaction.CommitAsync"/>
    /// to persist work; otherwise the transaction is rolled back when disposed.
    /// </summary>
    Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
