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

    public async Task<byte[]> RemoveBackgroundAsync(
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

    private async Task<byte[]> CallRemoveBgApiAsync(
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

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
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

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        return ex.StatusCode is HttpStatusCode.TooManyRequests
            or >= HttpStatusCode.InternalServerError;
    }
}
