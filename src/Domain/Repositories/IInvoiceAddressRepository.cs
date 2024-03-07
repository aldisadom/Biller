using Domain.Entities;

namespace Domain.Repositories;

public interface IInvoiceAddressRepository
{
    Task<InvoiceAddressEntity?> Get(Guid id);
    Task<IEnumerable<InvoiceAddressEntity>> GetByUser(Guid userId);
    Task<IEnumerable<InvoiceAddressEntity>> Get();
    Task<Guid> Add(InvoiceAddressEntity client);
    Task Update(InvoiceAddressEntity client);
    Task Delete(Guid id);
}
