using Application.Models;

namespace Application.Interfaces;

public interface IInvoiceClientService
{
    Task<Guid> Add(InvoiceClientModel invoiceClient);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceClientModel>> Get();
    Task<InvoiceClientModel> Get(Guid id);
    Task<IEnumerable<InvoiceClientModel>> GetByUser(Guid userId);
    Task Update(InvoiceClientModel invoiceClient);
}