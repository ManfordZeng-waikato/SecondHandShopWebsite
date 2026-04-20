using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure.UnitTests.Services;

public class TurnstileValidatorTests
{
    private const string VerifyUrl = "https://turnstile.example.test/siteverify";
    private const string SecretKey = "test-secret-key";

    [Fact]
    public async Task ValidateAsync_ShouldReturnMissingInputFailure_WhenTokenIsBlank()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent("{\"success\":true}") });
        var sut = CreateSut(handler);

        var result = await sut.ValidateAsync("   ", remoteIpAddress: "127.0.0.1");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCodes.Should().ContainSingle().Which.Should().Be("missing-input-response");
        handler.CallCount.Should().Be(0, "no HTTP call should be made for an empty token");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_AndPostSecretAndRemoteIp_WhenCloudflareApproves()
    {
        const string body = """
            {
                "success": true,
                "challenge_ts": "2026-04-20T09:00:00Z",
                "hostname": "example.test",
                "action": "submit-inquiry",
                "error-codes": []
            }
            """;

        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(body) });

        var sut = CreateSut(handler);

        var result = await sut.ValidateAsync("tok-123", remoteIpAddress: "203.0.113.7");

        result.IsSuccess.Should().BeTrue();
        result.Hostname.Should().Be("example.test");
        result.Action.Should().Be("submit-inquiry");
        result.ChallengeTs.Should().NotBeNull();

        handler.CallCount.Should().Be(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be(VerifyUrl);
        handler.LastRequestBody.Should().NotBeNull();
        handler.LastRequestBody!.Should().Contain("secret=" + SecretKey);
        handler.LastRequestBody.Should().Contain("response=tok-123");
        handler.LastRequestBody.Should().Contain("remoteip=203.0.113.7");
    }

    [Fact]
    public async Task ValidateAsync_ShouldOmitRemoteIpFromPayload_WhenNotProvided()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent("{\"success\":true}") });

        var sut = CreateSut(handler);

        var result = await sut.ValidateAsync("tok-abc", remoteIpAddress: null);

        result.IsSuccess.Should().BeTrue();
        handler.LastRequestBody.Should().NotBeNull();
        handler.LastRequestBody!.Should().NotContain("remoteip=");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailureWithErrorCodes_WhenCloudflareRejects()
    {
        const string body = """
            {
                "success": false,
                "error-codes": ["invalid-input-response", "timeout-or-duplicate"]
            }
            """;

        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(body) });

        var sut = CreateSut(handler);

        var result = await sut.ValidateAsync("bad-token", remoteIpAddress: "127.0.0.1");

        result.IsSuccess.Should().BeFalse();
        result.ErrorCodes.Should().BeEquivalentTo(new[] { "invalid-input-response", "timeout-or-duplicate" });
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowUnavailable_WhenSecretKeyIsMissing()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var options = new CloudflareTurnstileOptions
        {
            SecretKey = string.Empty,
            VerifyUrl = VerifyUrl
        };
        var sut = new TurnstileValidator(
            new HttpClient(handler),
            options,
            NullLogger<TurnstileValidator>.Instance);

        var act = () => sut.ValidateAsync("tok", remoteIpAddress: "127.0.0.1");

        await act.Should().ThrowAsync<TurnstileValidationUnavailableException>();
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowUnavailable_WhenCloudflareReturns5xx()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var sut = CreateSut(handler);

        var act = () => sut.ValidateAsync("tok", remoteIpAddress: "127.0.0.1");

        await act.Should().ThrowAsync<TurnstileValidationUnavailableException>();
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowUnavailable_WhenRequestTimesOut()
    {
        var handler = new RecordingHandler(_ =>
            throw new TaskCanceledException("Simulated timeout.", new TimeoutException()));
        var sut = CreateSut(handler);

        var act = () => sut.ValidateAsync("tok", remoteIpAddress: "127.0.0.1");

        await act.Should().ThrowAsync<TurnstileValidationUnavailableException>();
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowUnavailable_WhenResponsePayloadIsInvalidJson()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent("not-a-json-body") });
        var sut = CreateSut(handler);

        var act = () => sut.ValidateAsync("tok", remoteIpAddress: "127.0.0.1");

        await act.Should().ThrowAsync<TurnstileValidationUnavailableException>();
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowUnavailable_WhenResponseBodyIsEmpty()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent("null") });
        var sut = CreateSut(handler);

        var act = () => sut.ValidateAsync("tok", remoteIpAddress: "127.0.0.1");

        await act.Should().ThrowAsync<TurnstileValidationUnavailableException>();
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowUnavailable_WhenNetworkRequestFails()
    {
        var handler = new RecordingHandler(_ =>
            throw new HttpRequestException("DNS failure."));
        var sut = CreateSut(handler);

        var act = () => sut.ValidateAsync("tok", remoteIpAddress: "127.0.0.1");

        await act.Should().ThrowAsync<TurnstileValidationUnavailableException>();
    }

    private static TurnstileValidator CreateSut(RecordingHandler handler) =>
        new(
            new HttpClient(handler),
            new CloudflareTurnstileOptions { SecretKey = SecretKey, VerifyUrl = VerifyUrl },
            NullLogger<TurnstileValidator>.Instance);

    private static StringContent JsonContent(string body) =>
        new(body, Encoding.UTF8, "application/json");

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }
}
