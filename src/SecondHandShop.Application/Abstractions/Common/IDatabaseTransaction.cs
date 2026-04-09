namespace SecondHandShop.Application.Abstractions.Common;

/// <summary>
/// A database transaction that must be committed explicitly; otherwise it is rolled back on dispose.
/// </summary>
public interface IDatabaseTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
