using System.Net;
using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.ImageProcessing;

namespace SecondHandShop.Infrastructure.Services;

public sealed class RemoveBgService(
    HttpClient httpClient,
    RemoveBgOptions options,
    ILogger<RemoveBgService> logger) : IBackgroundRemovalService
{
    private const string RemoveBgEndpoint = "https://api.remove.bg/v1.0/removebg";

    public async Task<BackgroundRemovalResult> RemoveBackgroundAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        options.Validate();

        var totalAttempts = 1 + Math.Max(0, options.MaxRetries);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= totalAttempts; attempt++)
        {
            try
            {
                return await CallRemoveBgApiAsync(imageStream, fileName, contentType, cancellationToken);
            }
            catch (HttpRequestException ex) when (IsTransient(ex) && attempt < totalAttempts)
            {
                lastException = ex;
                logger.LogWarning(ex, "remove.bg transient failure on attempt {Attempt}/{Total}. Retrying...",
                    attempt, totalAttempts);
                imageStream.Position = 0;
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < totalAttempts)
            {
                lastException = ex;
                logger.LogWarning("remove.bg request timed out on attempt {Attempt}/{Total}. Retrying...",
                    attempt, totalAttempts);
                imageStream.Position = 0;
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Background removal failed after all retry attempts.", lastException);
    }

    private async Task<BackgroundRemovalResult> CallRemoveBgApiAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "image_file", fileName);
        content.Add(new StringContent("auto"), "size");

        using var request = new HttpRequestMessage(HttpMethod.Post, RemoveBgEndpoint)
        {
            Content = content
        };
        request.Headers.Add("X-Api-Key", options.ApiKey);

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("remove.bg returned {StatusCode}: {Body}", (int)response.StatusCode, errorBody);

                throw response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                        => new InvalidOperationException("Background removal service authentication failed. Check RemoveBg:ApiKey."),
                    HttpStatusCode.PaymentRequired
                        => new InvalidOperationException("Background removal credit limit reached."),
                    HttpStatusCode.UnprocessableEntity
                        => new InvalidOperationException("The image could not be processed. Please try a different image."),
                    HttpStatusCode.TooManyRequests
                        => new HttpRequestException("Rate limited by background removal service.", null, HttpStatusCode.TooManyRequests),
                    >= HttpStatusCode.InternalServerError
                        => new HttpRequestException($"Background removal service error ({(int)response.StatusCode}).", null, response.StatusCode),
                    _ => new InvalidOperationException($"Background removal failed ({(int)response.StatusCode}): {errorBody}")
                };
            }
            finally
            {
                response.Dispose();
            }
        }

        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new BackgroundRemovalResult(new ResponseContentStream(response, responseStream), "image/png");
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        return ex.StatusCode is HttpStatusCode.TooManyRequests
            or >= HttpStatusCode.InternalServerError;
    }

    private sealed class ResponseContentStream(HttpResponseMessage response, Stream innerStream) : Stream
    {
        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => innerStream.Length;

        public override long Position
        {
            get => innerStream.Position;
            set => innerStream.Position = value;
        }

        public override void Flush() => innerStream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => innerStream.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => innerStream.Read(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => innerStream.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);
        public override void SetLength(long value) => innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => innerStream.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => innerStream.WriteAsync(buffer, cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => innerStream.WriteAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
                response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await innerStream.DisposeAsync();
            response.Dispose();
            await base.DisposeAsync();
        }
    }
}
