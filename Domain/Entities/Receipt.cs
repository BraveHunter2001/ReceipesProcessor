public class Receipt
{
    public int Id {  get; set; }
    public decimal? TotalSum { get; set; }
    public string Currency { get; set; } = "RUB";
    public List<ReceiptItem>? Items { get; set; } 

    public ReceiptStatus Status { get; set; }
    public string? RawText { get; set; }
    public string? ImageId { get; set; }

    public required string OwnerKey { get; set; }
}

public enum ReceiptStatus
{
    Created,
    TextExctracted,
    Finished
}