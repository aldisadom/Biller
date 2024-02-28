using Domain.Entities;

namespace Domain.Repositories;

public interface IInvoiceItemRepository
{
    Task<InvoiceItemEntity?> Get(Guid id);
    Task<IEnumerable<InvoiceItemEntity>> GetByUser(Guid userId);
    Task<IEnumerable<InvoiceItemEntity>> Get();
    Task<Guid> Add(InvoiceItemEntity item);
    Task Update(InvoiceItemEntity item);
    Task Delete(Guid id);
}