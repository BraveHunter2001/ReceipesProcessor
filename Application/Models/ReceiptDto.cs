using System.Text.Json.Serialization;

public class ReceiptDto
{
    [JsonPropertyName("total_sum")] public decimal TotalSum { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "RUB";
    [JsonPropertyName("items")] public ReceiptItemDto[] Items { get; set; } = [];
}