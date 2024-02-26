using Application.Models;

namespace Application.Interfaces;

public interface IInvoiceItemService
{
    Task<Guid> Add(InvoiceItemModel item);
    Task Delete(Guid id);
    Task<IEnumerable<InvoiceItemModel>> Get();
    Task<InvoiceItemModel> Get(Guid id);
    Task Update(InvoiceItemModel item);
}