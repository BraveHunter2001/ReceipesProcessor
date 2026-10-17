using System.Text.Json.Serialization;
// Структура JSON объектов
public class ReceiptItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("quantity")] public double Quantity { get; set; }
    [JsonPropertyName("price")] public double Price { get; set; }
    [JsonPropertyName("sum")] public double Sum { get; set; }
}
