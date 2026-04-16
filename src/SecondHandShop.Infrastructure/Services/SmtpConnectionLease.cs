using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace SecondHandShop.Infrastructure.Services;

public sealed class SmtpConnectionLease(
    SmtpEmailOptions options,
    ILogger<SmtpConnectionLease> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private SmtpClient? _client;

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);
            await _client!.SendAsync(message, cancellationToken);
        }
        catch
        {
            await ResetClientAsync();
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _sendLock.WaitAsync();

        try
        {
            await DisposeClientAsync();
        }
        finally
        {
            _sendLock.Release();
            _sendLock.Dispose();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client is { IsConnected: true, IsAuthenticated: true })
        {
            return;
        }

        await DisposeClientAsync();

        var client = new SmtpClient();
        await client.ConnectAsync(
            options.Host,
            options.Port,
            ResolveSocketOptions(options),
            cancellationToken);
        await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);

        _client = client;

        logger.LogDebug(
            "Established SMTP connection to {Host}:{Port} using {SecurityMode}.",
            options.Host,
            options.Port,
            ResolveSocketOptions(options));
    }

    private async Task ResetClientAsync()
    {
        logger.LogWarning("SMTP connection became unusable; disposing current MailKit client.");
        await DisposeClientAsync();
    }

    private async Task DisposeClientAsync()
    {
        if (_client is null)
        {
            return;
        }

        try
        {
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync(quit: true, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "SMTP disconnect failed while disposing MailKit client.");
        }
        finally
        {
            _client.Dispose();
            _client = null;
        }
    }

    private static SecureSocketOptions ResolveSocketOptions(SmtpEmailOptions options)
    {
        if (!options.UseSsl)
        {
            return SecureSocketOptions.None;
        }

        return options.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }
}
