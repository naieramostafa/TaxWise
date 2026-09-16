using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using StreamlineTax.Application.Common.Interfaces;

namespace StreamlineTax.Infrastructure.Services;

public class S3FileStorageService : IFileStorageService, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private bool _disposed;

    public S3FileStorageService(IConfiguration configuration)
    {
        var accessKey = configuration["Storage:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["Storage:SecretKey"] ?? "minioadmin";
        var serviceUrl = configuration["Storage:ServiceUrl"] ?? "http://localhost:9000";

        _bucketName = configuration["Storage:BucketName"] ?? "streamline-tax-receipts";

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadAsync(Stream fileStream, string storageKey, string contentType, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            InputStream = fileStream,
            ContentType = contentType
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return storageKey;
    }

    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey
        };

        var response = await _s3Client.GetObjectAsync(request, cancellationToken);
        return response.ResponseStream;
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = storageKey
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = _bucketName
            }, cancellationToken);
        }
        catch (AmazonS3Exception)
        {
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _s3Client?.Dispose();
            }
            _disposed = true;
        }
    }
}
