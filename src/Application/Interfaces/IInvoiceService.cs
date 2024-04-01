using Application.Models;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task GeneratePDF(InvoiceDataModel invoiceModel);
}
