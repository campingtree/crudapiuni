namespace CrudApp.Options;

public sealed class S3StorageOptions
{
    public string Endpoint { get; set; } = "";
    public string BucketName { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
}
