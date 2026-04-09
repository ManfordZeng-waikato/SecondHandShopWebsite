using System.Threading.Channels;
using SecondHandShop.Application.Abstractions.Messaging;

namespace SecondHandShop.Infrastructure.Services;

/// <summary>
/// Channel-backed singleton implementation of <see cref="IInquiryDispatchSignal"/>. The channel
/// has capacity 1 with DropWrite, so repeated Notify calls coalesce into a single wake-up.
/// </summary>
public sealed class InquiryDispatchSignal : IInquiryDispatchSignal
{
    private readonly Channel<byte> _channel = Channel.CreateBounded<byte>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public void Notify() => _channel.Writer.TryWrite(0);

    public async Task WaitForNextAsync(TimeSpan fallback, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(fallback);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await _channel.Reader.ReadAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Fallback timeout fired — caller should proceed to scan for retries.
        }
    }
}
