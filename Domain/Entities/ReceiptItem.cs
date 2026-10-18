public class ReceiptItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Sum { get; set; }

    public int ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }
}
