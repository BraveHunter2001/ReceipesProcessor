namespace Application.Configuration;

public sealed class OllamaSettings
{
    public const string SectionName = "OllamaSettings";
    public const string DefaultBaseUrl = "http://localhost:11434";

    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
