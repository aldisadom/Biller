using Domain.Entities;

namespace Domain.Repositories;

public interface IInvoiceClientRepository
{
    Task<InvoiceClientEntity?> Get(Guid id);
    Task<IEnumerable<InvoiceClientEntity>> GetByUser(Guid userId);
    Task<IEnumerable<InvoiceClientEntity>> Get();
    Task<Guid> Add(InvoiceClientEntity client);
    Task Update(InvoiceClientEntity client);
    Task Delete(Guid id);
}
