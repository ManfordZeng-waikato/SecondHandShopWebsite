using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.Security;

namespace SecondHandShop.Infrastructure.Services;

public sealed class TurnstileValidator(
    HttpClient httpClient,
    CloudflareTurnstileOptions options,
    ILogger<TurnstileValidator> logger) : ITurnstileValidator
{
    private const string VerificationUnavailableMessage =
        "Security verification service is temporarily unavailable. Please try again later.";

    public async Task<TurnstileValidationResult> ValidateAsync(
        string token,
        string? remoteIpAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new TurnstileValidationResult
            {
                IsSuccess = false,
                ErrorCodes = ["missing-input-response"]
            };
        }

        try
        {
            options.ValidateForServer();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Cloudflare Turnstile configuration is invalid.");
            throw new TurnstileValidationUnavailableException(VerificationUnavailableMessage, ex);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, options.VerifyUrl)
        {
            Content = BuildRequestContent(token, remoteIpAddress)
        };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Cloudflare Turnstile verification request timed out.");
            throw new TurnstileValidationUnavailableException(VerificationUnavailableMessage, ex);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Cloudflare Turnstile verification request failed.");
            throw new TurnstileValidationUnavailableException(VerificationUnavailableMessage, ex);
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Cloudflare Turnstile verification returned unexpected status code {StatusCode}.",
                (int)response.StatusCode);
            throw new TurnstileValidationUnavailableException(VerificationUnavailableMessage);
        }

        TurnstileSiteVerifyResponse? verifyResponse;
        try
        {
            verifyResponse = await response.Content.ReadFromJsonAsync<TurnstileSiteVerifyResponse>(cancellationToken);
        }
        catch (NotSupportedException ex)
        {
            logger.LogError(ex, "Cloudflare Turnstile response content type is unsupported.");
            throw new TurnstileValidationUnavailableException(VerificationUnavailableMessage, ex);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Cloudflare Turnstile response payload is invalid.");
            throw new TurnstileValidationUnavailableException(VerificationUnavailableMessage, ex);
        }

        if (verifyResponse is null)
        {
            logger.LogError("Cloudflare Turnstile response payload is empty.");
            throw new TurnstileValidationUnavailableException(VerificationUnavailableMessage);
        }

        if (!verifyResponse.Success)
        {
            logger.LogWarning(
                "Cloudflare Turnstile verification failed. ErrorCodes={ErrorCodes}, Hostname={Hostname}, Action={Action}",
                string.Join(",", verifyResponse.ErrorCodes),
                verifyResponse.Hostname,
                verifyResponse.Action);
        }

        return new TurnstileValidationResult
        {
            IsSuccess = verifyResponse.Success,
            ErrorCodes = verifyResponse.ErrorCodes,
            Action = verifyResponse.Action,
            Hostname = verifyResponse.Hostname,
            ChallengeTs = verifyResponse.ChallengeTs
        };
    }

    private FormUrlEncodedContent BuildRequestContent(string token, string? remoteIpAddress)
    {
        var requestContent = new Dictionary<string, string>
        {
            ["secret"] = options.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
        {
            requestContent["remoteip"] = remoteIpAddress;
        }

        return new FormUrlEncodedContent(requestContent);
    }
}
