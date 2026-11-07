namespace ReceipesProcessor.Configuration;

public sealed class TelegramBotSettings
{
    public const string SectionName = "TelegramBot";

    public bool Enabled { get; set; }
    public string? Token { get; set; }
}
