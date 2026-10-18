namespace ReceipesProcessor.Response;

public class ReceiptsListItem
{
    public int Id { get; set; }
    public decimal? TotalSum { get; set; }
    public string Currency { get; set; } = "RUB";
    public List<ReceiptsItemListItem>? Items { get; set; }

    public ReceiptStatus Status { get; set; }
    public string OwnerKey { get; set; }

    public ReceiptsListItem(Receipt receipt)
    {
        Id = receipt.Id;
        TotalSum = receipt.TotalSum;
        Currency = receipt.Currency;
        Items = receipt.Items?.ConvertAll(r => new ReceiptsItemListItem(r));
        Status = receipt.Status;
        OwnerKey = receipt.OwnerKey;
    }
}

//todo: странное название
public class ReceiptsItemListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Sum { get; set; }

    public ReceiptsItemListItem(ReceiptItem receiptItem)
    {
        Id = receiptItem.Id;
        Name = receiptItem.Name;
        Quantity = receiptItem.Quantity;
        Price = receiptItem.Price;
        Sum = receiptItem.Sum;
    }
}