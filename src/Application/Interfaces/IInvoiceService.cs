using Application.Models;
using Contracts.Requests.Invoice;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Guid> Add(InvoiceModel invoiceData);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceModel>> Get(InvoiceGetRequest? query);
    Task<InvoiceModel> Get(Guid id);
    Task Update(InvoiceModel invoiceData);
    Task GeneratePDF(Guid id);
}
