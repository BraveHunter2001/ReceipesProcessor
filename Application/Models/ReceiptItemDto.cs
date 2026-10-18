using System.Text.Json.Serialization;

public class ReceiptItemDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("quantity")] public double Quantity { get; set; }
    [JsonPropertyName("price")] public decimal Price { get; set; }
    [JsonPropertyName("sum")] public decimal Sum { get; set; }
}
