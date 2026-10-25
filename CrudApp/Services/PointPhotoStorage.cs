using Amazon.S3;
using Amazon.S3.Model;
using CrudApp.Options;
using Microsoft.Extensions.Options;

namespace CrudApp.Services;

public sealed class PointPhotoStorage(IAmazonS3 client, IOptions<S3StorageOptions> options)
{
    public async Task<string> UploadAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        var key = Guid.NewGuid().ToString("N");
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        }, cancellationToken);
        return key;
    }

    public async Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        using var response = await client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key
        }, cancellationToken);
        await using var content = new MemoryStream();
        await response.ResponseStream.CopyToAsync(content, cancellationToken);
        return content.ToArray();
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = options.Value.BucketName,
            Key = key
        }, cancellationToken);
    }
}
