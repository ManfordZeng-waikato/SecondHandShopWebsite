using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Abstractions.ImageProcessing;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/admin/images")]
public class ImageProcessingController(
    IBackgroundRemovalService backgroundRemovalService,
    RemoveBgOptions removeBgOptions) : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    [HttpPost("remove-background-preview")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> RemoveBackgroundPreviewAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ErrorResponse("No image file provided."));
        }

        if (file.Length > removeBgOptions.MaxFileSizeBytes)
        {
            return BadRequest(new ErrorResponse(
                $"File size exceeds the {removeBgOptions.MaxFileSizeBytes / (1024 * 1024)} MB limit."));
        }

        var contentType = file.ContentType?.Trim() ?? string.Empty;
        if (!AllowedContentTypes.Contains(contentType))
        {
            return BadRequest(new ErrorResponse("Only JPEG, PNG and WEBP images are allowed."));
        }

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            var resultBytes = await backgroundRemovalService.RemoveBackgroundAsync(
                stream, file.FileName, contentType, cancellationToken);

            return File(resultBytes, "image/png", $"preview-nobg-{Path.GetFileNameWithoutExtension(file.FileName)}.png");
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new ErrorResponse(ex.Message));
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new ErrorResponse("Background removal service is temporarily unavailable. Please try again later."));
        }
    }
}
