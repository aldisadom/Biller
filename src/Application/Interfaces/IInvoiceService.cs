using Application.Models;
using Common.Enums;
using Contracts.Requests.Invoice;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Guid> Add(InvoiceModel invoiceData);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceModel>> Get(InvoiceGetRequest? query);
    Task<InvoiceModel> Get(Guid id);
    Task Update(InvoiceModel invoiceDetails);
    Task<FileStream> GeneratePDF(Guid id, Language languageCode, DocumentType documentType);
}
