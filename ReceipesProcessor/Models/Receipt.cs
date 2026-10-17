using System.Text.Json.Serialization;

public class Receipt
{
    [JsonPropertyName("total_sum")] public double TotalSum { get; set; }
    [JsonPropertyName("currency")] public string Currency { get; set; } = "RUB";
    [JsonPropertyName("items")] public ReceiptItem[] Items { get; set; } = [];
}