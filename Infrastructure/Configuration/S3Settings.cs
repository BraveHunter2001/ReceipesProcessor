namespace Infrastructure.Configuration;

public sealed class S3Settings
{
    public const string SectionName = "S3Settings";

    public string? ServiceUrl { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? BucketName { get; set; }
}
