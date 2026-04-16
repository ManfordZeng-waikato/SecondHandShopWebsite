using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Abstractions.ImageProcessing;
using SecondHandShop.Infrastructure.Services;
using SecondHandShop.WebApi.Contracts;
using SecondHandShop.WebApi.Utilities;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/images")]
[Authorize(Policy = "AdminFullAccess")]
public class ImageProcessingController(
    IBackgroundRemovalService backgroundRemovalService,
    RemoveBgOptions removeBgOptions) : ControllerBase
{
    private const int SignatureReadLength = 12;

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

        if (!AdminPreviewImageValidation.TryGetSafeFileName(file.FileName, out var safeFileName, out var declaredExtension))
        {
            return BadRequest(new ErrorResponse(
                "File name must use a .jpg, .jpeg, .png, or .webp extension and must not contain path segments."));
        }

        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            stream.Position = 0;

            var header = new byte[SignatureReadLength];
            var read = await stream.ReadAsync(header.AsMemory(0, SignatureReadLength), cancellationToken);
            stream.Position = 0;

            if (!AdminPreviewImageValidation.TryDetectImageFormat(header.AsSpan(0, read), out var verifiedContentType, out var signatureExtension)
                || !AdminPreviewImageValidation.ExtensionMatchesSignature(declaredExtension, signatureExtension))
            {
                return BadRequest(new ErrorResponse(
                    "Only valid JPEG, PNG, or WEBP image data is allowed (content must match extension)."));
            }

            var baseName = Path.GetFileNameWithoutExtension(safeFileName);
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "image";
            }

            if (baseName.Length > 120)
            {
                baseName = baseName[..120];
            }

            var multipartFileName = baseName + signatureExtension;

            await using var result = await backgroundRemovalService.RemoveBackgroundAsync(
                stream, multipartFileName, verifiedContentType, cancellationToken);

            Response.ContentType = result.ContentType;
            Response.Headers.ContentDisposition = $"attachment; filename=\"preview-nobg-{baseName}.png\"";
            await result.Content.CopyToAsync(Response.Body, cancellationToken);
            return new EmptyResult();
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
