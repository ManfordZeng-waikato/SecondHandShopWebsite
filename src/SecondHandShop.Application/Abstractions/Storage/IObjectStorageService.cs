namespace SecondHandShop.Application.Abstractions.Storage;

public interface IObjectStorageService
{
    Task<PresignedUploadUrlResult> CreatePresignedUploadUrlAsync(
        PresignedUploadUrlRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default);

    string BuildDisplayUrl(string objectKey);
}

public sealed record PresignedUploadUrlRequest(
    string ObjectKey,
    string ContentType,
    TimeSpan ExpiresIn);

public sealed record PresignedUploadUrlResult(
    string UploadUrl,
    DateTime ExpiresAtUtc);
