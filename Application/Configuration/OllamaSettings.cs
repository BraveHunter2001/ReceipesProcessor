namespace Application.Configuration;

public sealed class OllamaSettings
{
    public const string SectionName = "OllamaSettings";
    public const string DefaultBaseUrl = "http://localhost:11434";

    public string BaseUrl { get; set; } = DefaultBaseUrl;
    public string Model { get; set; } = "qwen2.5:3b";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);
}
