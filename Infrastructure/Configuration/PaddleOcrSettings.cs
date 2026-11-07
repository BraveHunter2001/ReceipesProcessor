namespace Infrastructure.Configuration;

public sealed class PaddleOcrSettings
{
    public const string SectionName = "PaddleOcr";
    public const string DefaultBaseUrl = "http://localhost:8000";

    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);
}
