using Application.Models;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Guid> Add(InvoiceDataModel invoiceModel);
}
