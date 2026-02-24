using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using SecondHandShop.Application.Abstractions.Storage;

namespace SecondHandShop.Infrastructure.Services;

public sealed class R2ObjectStorageService(R2Options options) : IObjectStorageService
{
    public async Task<PresignedUploadUrlResult> CreatePresignedUploadUrlAsync(
        PresignedUploadUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        options.Validate();

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
        {
            throw new ArgumentException("Object key is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ArgumentException("Content type is required.", nameof(request));
        }

        var endpoint = $"https://{options.AccountId}.r2.cloudflarestorage.com";
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true
        };

        using var client = new AmazonS3Client(credentials, config);

        var expiresAtUtc = DateTime.UtcNow.Add(request.ExpiresIn <= TimeSpan.Zero ? TimeSpan.FromMinutes(10) : request.ExpiresIn);
        var preSignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = options.BucketName,
            Key = request.ObjectKey,
            Verb = HttpVerb.PUT,
            ContentType = request.ContentType,
            Expires = expiresAtUtc
        };

        var url = await client.GetPreSignedURLAsync(preSignedRequest);
        return new PresignedUploadUrlResult(url, expiresAtUtc);
    }

    public string BuildPublicUrl(string objectKey)
    {
        options.Validate();
        var trimmedBaseUrl = options.PublicBaseUrl.TrimEnd('/');
        return $"{trimmedBaseUrl}/{objectKey}";
    }
}
