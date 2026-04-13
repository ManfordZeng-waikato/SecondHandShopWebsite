using System.Threading.Channels;
using SecondHandShop.Application.Abstractions.Messaging;

namespace SecondHandShop.Infrastructure.Services;

public sealed class AdminLoginNotificationQueue : IAdminLoginNotificationQueue
{
    private readonly Channel<AdminLoginNotificationMessage> _channel = Channel.CreateBounded<AdminLoginNotificationMessage>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public void Enqueue(AdminLoginNotificationMessage message)
    {
        _channel.Writer.TryWrite(message);
    }

    public ValueTask<AdminLoginNotificationMessage> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
