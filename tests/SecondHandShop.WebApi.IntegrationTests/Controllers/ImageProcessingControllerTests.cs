using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.ImageProcessing;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class ImageProcessingControllerTests
{
    private static readonly byte[] PngHeader =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly TestWebApplicationFactory _factory;

    public ImageProcessingControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task RemoveBackgroundPreview_ShouldReturn401_WhenAnonymous()
    {
        using var client = CreateClient();
        using var form = CreateImageForm(PngPayload(), "photo.png", "image/png");

        var response = await client.PostAsync("/api/lord/images/remove-background-preview", form);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveBackgroundPreview_ShouldReturn400_WhenExtensionIsNotAllowed()
    {
        using var client = CreateAuthenticatedClient();
        using var form = CreateImageForm(PngPayload(), "photo.gif", "image/gif");

        var response = await client.PostAsync("/api/lord/images/remove-background-preview", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(".jpg", "error must enumerate the supported extensions");
    }

    [Fact]
    public async Task RemoveBackgroundPreview_ShouldReturn400_WhenSignatureDoesNotMatchExtension()
    {
        using var client = CreateAuthenticatedClient();
        // Claim ".png" but payload has neither PNG nor JPEG nor WEBP magic bytes.
        var junkPayload = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C };
        using var form = CreateImageForm(junkPayload, "photo.png", "image/png");

        var response = await client.PostAsync("/api/lord/images/remove-background-preview", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Only valid JPEG, PNG, or WEBP image data is allowed");
    }

    [Fact]
    public async Task RemoveBackgroundPreview_ShouldReturn502_WhenUpstreamServiceFails()
    {
        _factory.BackgroundRemovalServiceMock
            .Setup(x => x.RemoveBackgroundAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("remove.bg upstream 5xx"));

        using var client = CreateAuthenticatedClient();
        using var form = CreateImageForm(PngPayload(), "photo.png", "image/png");

        var response = await client.PostAsync("/api/lord/images/remove-background-preview", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task RemoveBackgroundPreview_ShouldReturn200_WithProcessedImage_WhenServiceSucceeds()
    {
        var resultBytes = new byte[] { 0xAA, 0xBB, 0xCC };
        _factory.BackgroundRemovalServiceMock
            .Setup(x => x.RemoveBackgroundAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new BackgroundRemovalResult(new MemoryStream(resultBytes), "image/png"));

        using var client = CreateAuthenticatedClient();
        using var form = CreateImageForm(PngPayload(), "photo.png", "image/png");

        var response = await client.PostAsync("/api/lord/images/remove-background-preview", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        response.Content.Headers.ContentDisposition!.FileName.Should().Contain("preview-nobg-photo");
        var body = await response.Content.ReadAsByteArrayAsync();
        body.Should().Equal(resultBytes);
    }

    private static byte[] PngPayload()
    {
        // 8-byte PNG signature + ~50 bytes of filler so the stream is seekable and not empty.
        var payload = new byte[64];
        Array.Copy(PngHeader, payload, PngHeader.Length);
        return payload;
    }

    private static MultipartFormDataContent CreateImageForm(byte[] bytes, string fileName, string contentType)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return form;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    private HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", TestWebApplicationFactory.CreateCookieHeader(
            _factory.CreateAdminToken()));
        return client;
    }
}
