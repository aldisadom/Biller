using Application.Models;
using BillerContracts.Enums;
using BillerContracts.Requests.Invoice;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Guid> Add(InvoiceModel invoiceData);
    Task Delete(Guid id);
    Task<(IEnumerable<InvoiceModelFull> Items, int TotalCount, int Page)> Get(InvoiceGetRequest? query);
    Task<InvoiceModelFull> Get(Guid id);
    Task Update(InvoiceModel invoiceDetails);
    Task UpdateStatus(InvoiceUpdateStatusRequest invoiceDetails);
    Task<(MemoryStream, string)> GeneratePDF(Guid id, Language languageCode, DocumentType documentType);
}
