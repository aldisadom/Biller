namespace Domain.Entities;

public record InvoiceItemEntity
{
    public Guid InvoiceId { get; set; }
    public int LineNo { get; set; }
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Comments { get; set; }
    // Read from the item (join); not stored in invoice_items
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
