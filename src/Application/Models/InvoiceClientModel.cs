namespace Application.Models;

public class InvoiceClientModel
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public object Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
