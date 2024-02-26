using Domain.Entities;

namespace Domain.Interfaces;

public interface IInvoiceItemRepository
{
    Task<InvoiceItemEntity?> Get(Guid id);
    Task<IEnumerable<InvoiceItemEntity>> GetByUser(Guid userId);
    Task<IEnumerable<InvoiceItemEntity>> Get();
    Task<Guid> Add(InvoiceItemEntity item);
    Task Update(InvoiceItemEntity item);
    Task Delete(Guid id);
}