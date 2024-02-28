using Application.Models;
using Contracts.Requests.InvoiceClient;

namespace Application.Interfaces;

public interface IInvoiceClientService
{
    Task<Guid> Add(InvoiceClientModel invoiceClient);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceClientModel>> Get(InvoiceClientGetRequest query);
    Task<InvoiceClientModel> Get(Guid id);
    Task Update(InvoiceClientModel invoiceClient);
}