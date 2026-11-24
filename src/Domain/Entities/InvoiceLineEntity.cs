namespace Domain.Entities;

public record InvoiceLineEntity
{
    public Guid InvoiceId { get; set; }
    public int LineNo { get; set; }
    public Guid ItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Comments { get; set; }
    // Read from the line's item (join); not stored on the line
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
