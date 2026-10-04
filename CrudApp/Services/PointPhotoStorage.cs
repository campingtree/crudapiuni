using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CrudApp.Options;
using Microsoft.Extensions.Options;

namespace CrudApp.Services;

public sealed class PointPhotoStorage(BlobServiceClient client, IOptions<BlobStorageOptions> options)
{
    private BlobContainerClient Container => client.GetBlobContainerClient(options.Value.ContainerName);

    public async Task<string> UploadAsync(Stream content, string contentType, CancellationToken cancellationToken)
    {
        var name = Guid.NewGuid().ToString("N");
        var container = Container;
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await container.GetBlobClient(name).UploadAsync(content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return name;
    }

    public async Task<BlobDownloadStreamingResult> DownloadAsync(string name, CancellationToken cancellationToken) =>
        (await Container.GetBlobClient(name).DownloadStreamingAsync(cancellationToken: cancellationToken)).Value;

    public Task DeleteAsync(string name, CancellationToken cancellationToken) =>
        Container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: cancellationToken);
}
