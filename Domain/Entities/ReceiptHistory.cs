public class ReceiptHistory
{
    public int Id { get; set; }
    //todo: мейби не верно тут писать статус чека, а миенно нужны какието мб отдельные действия над чеком?
    public ReceiptStatus Status {  get; set; }
    public DateTime AtRecord { get; set; }

    public int ReceiptId { get; set; }
    public Receipt? Receipt { get; set; }
}
