using Application.Models;
using Contracts.Requests.InvoiceItem;

namespace Application.Interfaces;

public interface IInvoiceItemService
{
    Task<Guid> Add(InvoiceItemModel item);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceItemModel>> Get(InvoiceItemGetRequest query);
    Task<IEnumerable<InvoiceItemModel>> Get(List<Guid> ids);
    Task<InvoiceItemModel> Get(Guid id);
    Task Update(InvoiceItemModel item);
}
