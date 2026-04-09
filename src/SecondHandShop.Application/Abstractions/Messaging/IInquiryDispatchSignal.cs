namespace SecondHandShop.Application.Abstractions.Messaging;

/// <summary>
/// Signal bridge between the request pipeline (which persists new inquiries as Pending) and
/// the background email dispatcher. Notifying wakes the dispatcher immediately; otherwise it
/// falls back to a periodic poll for retries.
/// </summary>
public interface IInquiryDispatchSignal
{
    /// <summary>Wake the dispatcher now. Safe to call from any thread; coalesces if already signalled.</summary>
    void Notify();

    /// <summary>
    /// Waits until either a <see cref="Notify"/> arrives or <paramref name="fallback"/> elapses.
    /// Returns without throwing on timeout; throws <see cref="OperationCanceledException"/> only if
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    Task WaitForNextAsync(TimeSpan fallback, CancellationToken cancellationToken);
}
