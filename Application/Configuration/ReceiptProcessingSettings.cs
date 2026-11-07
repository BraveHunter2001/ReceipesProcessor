namespace Application.Configuration;

public sealed class ReceiptProcessingSettings
{
    public const string SectionName = "ReceiptProcessing";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int BatchSize { get; set; } = 50;
}
