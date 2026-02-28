using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using SecondHandShop.Application.Abstractions.Storage;

namespace SecondHandShop.Infrastructure.Services;

public sealed class R2ObjectStorageService(R2Options options) : IObjectStorageService
{
    private static readonly TimeSpan DefaultPresignExpiry = TimeSpan.FromMinutes(5);

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

        using var client = CreateS3Client();

        var expiry = request.ExpiresIn <= TimeSpan.Zero ? DefaultPresignExpiry : request.ExpiresIn;
        var expiresAtUtc = DateTime.UtcNow.Add(expiry);

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

    public async Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        options.Validate();

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("Object key is required.", nameof(objectKey));
        }

        using var client = CreateS3Client();

        await client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = options.BucketName,
            Key = objectKey
        }, cancellationToken);
    }

    public string BuildDisplayUrl(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(options.WorkerBaseUrl))
        {
            throw new InvalidOperationException("R2:WorkerBaseUrl is required.");
        }

        var trimmedBaseUrl = options.WorkerBaseUrl.TrimEnd('/');
        return $"{trimmedBaseUrl}/{objectKey}";
    }

    private AmazonS3Client CreateS3Client()
    {
        var endpoint = $"https://{options.AccountId}.r2.cloudflarestorage.com";
        var credentials = new BasicAWSCredentials(options.AccessKeyId, options.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true
        };
        return new AmazonS3Client(credentials, config);
    }
}
