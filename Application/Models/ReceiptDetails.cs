namespace Application.Models;

public sealed class ReceiptDetails
{
    public int Id { get; init; }
    public decimal? TotalSum { get; init; }
    public string Currency { get; init; } = "RUB";
    public ReceiptStatus Status { get; init; }
    public string? RawText { get; init; }
    public List<ReceiptsItemListItem> Items { get; init; } = [];
    public List<ReceiptStatusHistoryItem> History { get; init; } = [];

    public ReceiptDetails(Receipt receipt, IEnumerable<ReceiptHistory> history)
    {
        Id = receipt.Id;
        TotalSum = receipt.TotalSum;
        Currency = receipt.Currency;
        Status = receipt.Status;
        RawText = receipt.RawText;
        Items = receipt.Items?.ConvertAll(item => new ReceiptsItemListItem(item)) ?? [];
        History = history
            .OrderByDescending(item => item.AtRecord)
            .Select(item => new ReceiptStatusHistoryItem(item.Status, item.AtRecord))
            .ToList();
    }
}

public sealed class ReceiptStatusHistoryItem
{
    public ReceiptStatus Status { get; init; }
    public DateTime AtRecord { get; init; }

    public ReceiptStatusHistoryItem(ReceiptStatus status, DateTime atRecord)
    {
        Status = status;
        AtRecord = atRecord;
    }
}
