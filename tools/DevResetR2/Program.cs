// One-off developer tool: enumerate every object under the "products/" prefix
// in the configured R2 bucket and delete them. Useful after running
// scripts/dev-reset-data.sql to wipe orphan images left behind.
//
// Hard safety rules:
//   - Requires the operator to type "DELETE" to confirm (no flags skip this).
//   - Only touches the "products/" prefix, never the bucket root.
//   - Never deletes the bucket itself.
//   - Prints every planned deletion before acting.
//
// Run from repo root:
//     dotnet run --project tools/DevResetR2

using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

const string ProductsPrefix = "products/";

var webApiBasePath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "SecondHandShop.WebApi"));

var configuration = new ConfigurationBuilder()
    .SetBasePath(webApiBasePath)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var accountId = configuration["R2:AccountId"];
var accessKeyId = configuration["R2:AccessKeyId"];
var secretAccessKey = configuration["R2:SecretAccessKey"];
var bucketName = configuration["R2:BucketName"];

if (string.IsNullOrWhiteSpace(accountId)
    || string.IsNullOrWhiteSpace(accessKeyId)
    || string.IsNullOrWhiteSpace(secretAccessKey)
    || string.IsNullOrWhiteSpace(bucketName))
{
    Console.Error.WriteLine("R2 configuration is incomplete.");
    Console.Error.WriteLine($"  AccountId     present: {!string.IsNullOrWhiteSpace(accountId)}");
    Console.Error.WriteLine($"  AccessKeyId   present: {!string.IsNullOrWhiteSpace(accessKeyId)}");
    Console.Error.WriteLine($"  SecretAccess  present: {!string.IsNullOrWhiteSpace(secretAccessKey)}");
    Console.Error.WriteLine($"  BucketName    present: {!string.IsNullOrWhiteSpace(bucketName)}");
    Console.Error.WriteLine("Make sure you run this from the repo root and that R2:* are set in user secrets or appsettings.Development.json.");
    return 2;
}

Console.WriteLine($"Target bucket : {bucketName}");
Console.WriteLine($"Target prefix : {ProductsPrefix}");
Console.WriteLine($"R2 endpoint   : https://{accountId}.r2.cloudflarestorage.com");
Console.WriteLine();

using var s3 = new AmazonS3Client(
    new BasicAWSCredentials(accessKeyId, secretAccessKey),
    new AmazonS3Config
    {
        ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
        ForcePathStyle = true
    });

var keys = new List<string>();
string? continuationToken = null;
do
{
    var response = await s3.ListObjectsV2Async(new ListObjectsV2Request
    {
        BucketName = bucketName,
        Prefix = ProductsPrefix,
        ContinuationToken = continuationToken
    });

    foreach (var obj in response.S3Objects)
    {
        keys.Add(obj.Key);
    }

    continuationToken = response.IsTruncated == true ? response.NextContinuationToken : null;
}
while (continuationToken is not null);

if (keys.Count == 0)
{
    Console.WriteLine("No objects under the products/ prefix. Nothing to delete.");
    return 0;
}

Console.WriteLine($"Found {keys.Count} object(s) to delete:");
foreach (var key in keys)
{
    Console.WriteLine($"  - {key}");
}
Console.WriteLine();
Console.Write("Type DELETE (in capitals) to confirm permanent deletion: ");
var confirmation = Console.ReadLine();
if (confirmation != "DELETE")
{
    Console.WriteLine("Aborted. No objects were deleted.");
    return 1;
}

// S3 DeleteObjects caps at 1000 keys per request; chunk defensively.
const int batchSize = 1000;
var deleted = 0;
for (var offset = 0; offset < keys.Count; offset += batchSize)
{
    var batch = keys.Skip(offset).Take(batchSize)
        .Select(k => new KeyVersion { Key = k })
        .ToList();

    var response = await s3.DeleteObjectsAsync(new DeleteObjectsRequest
    {
        BucketName = bucketName,
        Objects = batch,
        Quiet = false
    });

    deleted += response.DeletedObjects?.Count ?? 0;

    if (response.DeleteErrors is { Count: > 0 })
    {
        Console.Error.WriteLine($"Encountered {response.DeleteErrors.Count} deletion error(s):");
        foreach (var error in response.DeleteErrors)
        {
            Console.Error.WriteLine($"  - {error.Key}: {error.Code} {error.Message}");
        }
    }
}

Console.WriteLine($"Done. Deleted {deleted} of {keys.Count} object(s).");
return deleted == keys.Count ? 0 : 3;
